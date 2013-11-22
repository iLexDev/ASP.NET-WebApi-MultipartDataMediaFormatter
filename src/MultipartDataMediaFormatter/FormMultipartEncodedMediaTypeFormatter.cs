using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MultipartDataMediaFormatter.Converters;
using MultipartDataMediaFormatter.Infrastructure;
using MultipartDataMediaFormatter.Infrastructure.Logger;

namespace MultipartDataMediaFormatter
{
    public class FormMultipartEncodedMediaTypeFormatter : MediaTypeFormatter
    {
        public FormMultipartEncodedMediaTypeFormatter()
        {
            var mediaTypeHeaderValue = new MediaTypeHeaderValue("multipart/form-data");
            mediaTypeHeaderValue.Parameters.Add(new NameValueHeaderValue("boundary", "MultipartDataMediaFormatterBoundary1q2w3e"));

            SupportedMediaTypes.Add(mediaTypeHeaderValue);
        }

        public override bool CanReadType(Type type)
        {
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }

        public override async Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content,
                                                               IFormatterLogger formatterLogger)
        {
            var httpContentToFormDataConverter = new HttpContentToFormDataConverter();
            FormData multipartFormData = await httpContentToFormDataConverter.Convert(content);

            IFormDataConverterLogger logger;
            if (formatterLogger != null)
                logger = new FormatterLoggerAdapter(formatterLogger);
            else 
                logger = new FormDataConverterLogger();

            var dataToObjectConverter = new FormDataToObjectConverter(multipartFormData, logger);
            object result = dataToObjectConverter.Convert(type);

            logger.EnsureNoErrors();

            return result;
        }

        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content,
                                                TransportContext transportContext)
        {
            return Task.Run(() =>
            {
                if (!content.IsMimeMultipartContent())
                {
                    throw new Exception("Unsupported Media Type");
                }

                var boudaryParameter = content.Headers.ContentType.Parameters.FirstOrDefault(m => m.Name == "boundary" && !String.IsNullOrWhiteSpace(m.Value));
                if(boudaryParameter == null)
                    throw new Exception("No boundary was found");

                var objectToMultipartDataByteArrayConverter = new ObjectToMultipartDataByteArrayConverter();
                byte[] multipartData = objectToMultipartDataByteArrayConverter.Convert(value, boudaryParameter.Value);

                writeStream.Write(multipartData, 0, multipartData.Length);

                content.Headers.ContentLength = multipartData.Length;
            });
        }
    }
}