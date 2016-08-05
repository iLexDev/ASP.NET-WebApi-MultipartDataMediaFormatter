using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MultipartDataMediaFormatter.Infrastructure;
using MultipartDataMediaFormatter.Infrastructure.Extensions;
using MultipartDataMediaFormatter.Infrastructure.Logger;

namespace MultipartDataMediaFormatter.Converters
{
    public class FormDataToObjectConverter
    {
        private readonly FormData SourceData;
        private readonly IFormDataConverterLogger Logger;
        private const string Undefined = "undefined";

        public FormDataToObjectConverter(FormData sourceData, IFormDataConverterLogger logger)
        {
            if (sourceData == null)
                throw new ArgumentNullException("sourceData");
            if (logger == null)
                throw new ArgumentNullException("logger");

            SourceData = sourceData;
            Logger = logger;
        }

        public object Convert(Type destinationType)
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");

            if (destinationType == typeof(FormData))
                return SourceData;

            var objResult = CreateObject(destinationType);
            return objResult;
        }

        private object CreateObject(Type destinationType, string propertyName = "")
        {
            object propValue = null;

            object buf;
            if (TryGetFromFormData(destinationType, propertyName, out buf)
                || TryGetAsGenericDictionary(destinationType, propertyName, out buf)
                || TryGetAsGenericListOrArray(destinationType, propertyName, out buf)
                || TryGetAsCustomType(destinationType, propertyName, out buf))
            {
                propValue = buf;
            }
            else if (!IsFileOrConvertableFromString(destinationType))
            {
                Logger.LogError(propertyName, String.Format("Cannot parse type \"{0}\".", destinationType.FullName));
            }

            return propValue;
        }

        private bool TryGetFromFormData(Type destinationType, string propertyName, out object propValue)
        {
            bool existsInFormData = false;
            propValue = null;

            if (destinationType == typeof(HttpFile))
            {
                HttpFile httpFile;
                if (SourceData.TryGetValue(propertyName, out httpFile))
                {
                    propValue = httpFile;
                    existsInFormData = true;
                }
            }
            else
            {
                string val;
                if (SourceData.TryGetValue(propertyName, out val))
                {
                    existsInFormData = true;
                    var typeConverter = destinationType.GetFromStringConverter();
                    if (typeConverter == null)
                    {
                        Logger.LogError(propertyName, "Cannot find type converter for field - " + propertyName);
                    }
                    else
                    {
                        if (Nullable.GetUnderlyingType(destinationType) != null && val == Undefined)
                        {
                            return true;
                        }

                        try
                        {
                            propValue = typeConverter.ConvertFromString(null, CultureInfo.InvariantCulture, val);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(propertyName, String.Format("Error parsing field \"{0}\": {1}", propertyName, ex.Message));
                        }
                    }
                }
            }

            return existsInFormData;
        }

        private bool TryGetAsGenericDictionary(Type destinationType, string propertyName, out object propValue)
        {
            propValue = null;
            Type keyType, valueType;
            bool isGenericDictionary = IsGenericDictionary(destinationType, out keyType, out valueType);
            if (isGenericDictionary)
            {
                var dictType = typeof(Dictionary<,>).MakeGenericType(new[] { keyType, valueType });
                var add = dictType.GetMethod("Add");

                var pValue = Activator.CreateInstance(dictType);

                int index = 0;
                string origPropName = propertyName;
                bool isFilled = false;
                while (true)
                {
                    string propertyKeyName = String.Format("{0}[{1}].Key", origPropName, index);
                    var objKey = CreateObject(keyType, propertyKeyName);
                    if (objKey != null)
                    {
                        string propertyValueName = String.Format("{0}[{1}].Value", origPropName, index);
                        var objValue = CreateObject(valueType, propertyValueName);

                        if (objValue != null)
                        {
                            add.Invoke(pValue, new[] { objKey, objValue });
                            isFilled = true;
                        }
                    }
                    else
                    {
                        break;
                    }
                    index++;
                }

                if (isFilled)
                {
                    propValue = pValue;
                }
            }

            return isGenericDictionary;
        }

        private bool TryGetAsGenericListOrArray(Type destinationType, string propertyName, out object propValue)
        {
            propValue = null;
            Type genericListItemType;
            bool isGenericList = IsGenericListOrArray(destinationType, out genericListItemType);
            if (isGenericList)
            {
                var listType = typeof(List<>).MakeGenericType(genericListItemType);

                var add = listType.GetMethod("Add");
                var pValue = Activator.CreateInstance(listType);

                int index = 0;
                string origPropName = propertyName;
                bool isFilled = false;
                while (true)
                {
                    propertyName = String.Format("{0}[{1}]", origPropName, index);
                    var objValue = CreateObject(genericListItemType, propertyName);
                    if (objValue != null)
                    {
                        add.Invoke(pValue, new[] { objValue });
                        isFilled = true;
                    }
                    else
                    {
                        break;
                    }

                    index++;
                }

                if (isFilled)
                {
                    if (destinationType.IsArray)
                    {
                        var toArrayMethod = listType.GetMethod("ToArray");
                        propValue = toArrayMethod.Invoke(pValue, new object[0]);
                    }
                    else
                    {
                        propValue = pValue;
                    }
                }
            }

            return isGenericList;
        }

        private bool TryGetAsCustomType(Type destinationType, string propertyName, out object propValue)
        {
            propValue = null;
            bool isCustomNonEnumerableType = destinationType.IsCustomNonEnumerableType();
            if (isCustomNonEnumerableType)
            {
                if (String.IsNullOrWhiteSpace(propertyName)
                    || SourceData.AllKeys().Any(m => m.StartsWith(propertyName + ".", StringComparison.CurrentCultureIgnoreCase)))
                {
                    var obj = Activator.CreateInstance(destinationType);
                    bool isFilled = false;
                    foreach (PropertyInfo propertyInfo in destinationType.GetPublicAccessibleProperties())
                    {
                        var propName = (!String.IsNullOrEmpty(propertyName) ? propertyName + "." : "") + propertyInfo.Name;
                        var objValue = CreateObject(propertyInfo.PropertyType, propName);
                        if (objValue != null)
                        {
                            propertyInfo.SetValue(obj, objValue);
                            isFilled = true;
                        }
                    }
                    if (isFilled)
                    {
                        propValue = obj;
                    }
                }
            }
            return isCustomNonEnumerableType;
        }


        private bool IsGenericDictionary(Type type, out Type keyType, out Type valueType)
        {
            Type iDictType = type.GetInterface(typeof(IDictionary<,>).Name);
            if (iDictType != null)
            {
                var types = iDictType.GetGenericArguments();
                if (types.Length == 2)
                {
                    keyType = types[0];
                    valueType = types[1];
                    return true;
                }
            }

            keyType = null;
            valueType = null;
            return false;
        }

        private bool IsGenericListOrArray(Type type, out Type itemType)
        {
            if (type.GetInterface(typeof(IDictionary<,>).Name) == null) //not a dictionary
            {
                if (type.IsArray)
                {
                    itemType = type.GetElementType();
                    return true;
                }

                Type iListType = type.GetInterface(typeof(ICollection<>).Name);
                if (iListType != null)
                {
                    Type[] genericArguments = iListType.GetGenericArguments();
                    if (genericArguments.Length == 1)
                    {
                        itemType = genericArguments[0];
                        return true;
                    }
                }
            }

            itemType = null;
            return false;
        }

        private bool IsFileOrConvertableFromString(Type type)
        {
            if (type == typeof(HttpFile))
                return true;

            return type.GetFromStringConverter() != null;
        }
    }
}