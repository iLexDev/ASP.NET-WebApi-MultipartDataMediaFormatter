using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace MultipartDataMediaFormatter.Infrastructure.Extensions
{
    internal static class TypeExtensions
    {
        internal static TypeConverter GetFromStringConverter(this Type type)
        {
            TypeConverter typeConverter = TypeDescriptor.GetConverter(type);
            if (typeConverter != null && !typeConverter.CanConvertFrom(typeof(String)))
            {
                typeConverter = null;
            }
            return typeConverter;
        }

        internal static TypeConverter GetToStringConverter(this Type type)
        {
            TypeConverter typeConverter = TypeDescriptor.GetConverter(type);
            if (typeConverter is DateTimeConverter)
            {
                //replace default datetime converter for serializing datetime in ISO 8601 format
                typeConverter = new DateTimeConverterISO8601();
            }
            if (typeConverter != null && !typeConverter.CanConvertTo(typeof(String)))
            {
                typeConverter = null;
            }
            return typeConverter;
        }

        internal static IEnumerable<PropertyInfo> GetPublicAccessibleProperties(this Type type)
        {
            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                if (!propertyInfo.CanRead || !propertyInfo.CanWrite || propertyInfo.SetMethod == null || propertyInfo.SetMethod.IsPrivate)
                    continue;
                yield return propertyInfo;
            }
        }

        internal static bool IsCustomNonEnumerableType(this Type type)
        {
            var nullType = Nullable.GetUnderlyingType(type);
            if (nullType != null)
            {
                type = nullType;
            }
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }
            return type != typeof(object)
                   && Type.GetTypeCode(type) == TypeCode.Object
                   && type != typeof(HttpFile)
                   && type != typeof(Guid)
                   && type.GetInterface(typeof(IEnumerable).Name) == null;
        }
    }
}
