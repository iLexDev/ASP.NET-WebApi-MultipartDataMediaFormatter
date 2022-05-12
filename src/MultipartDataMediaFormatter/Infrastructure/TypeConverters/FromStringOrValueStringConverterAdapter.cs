using System;
using System.ComponentModel;
using System.Globalization;

namespace MultipartDataMediaFormatter.Infrastructure.TypeConverters
{
    public class FromStringOrValueStringConverterAdapter
    {
        private readonly Type Type;
        private readonly TypeConverter TypeConverter;

        public FromStringOrValueStringConverterAdapter(Type type, TypeConverter typeConverter)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (typeConverter == null)
                throw new ArgumentNullException("typeConverter");

            Type = type;
            TypeConverter = typeConverter;
        }

        public object ConvertFromString(string src, CultureInfo culture)
        {
            var isUndefinedNullable = Nullable.GetUnderlyingType(Type) != null && src == "undefined";
            if (isUndefinedNullable)
                return null;

            return TypeConverter.ConvertFromString(null, culture, src);
        }

        public object ConvertFromValueString(FormData.ValueString[] src, CultureInfo culture)
        {
            var isUndefinedNullable = Nullable.GetUnderlyingType(Type) != null;
            if (isUndefinedNullable)
                return null;

            return TypeConverter.ConvertFrom(null, culture, src);
        }
    }
}
