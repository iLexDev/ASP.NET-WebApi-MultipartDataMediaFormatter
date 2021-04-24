using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MultipartDataMediaFormatter.Infrastructure;

namespace MultipartDataMediaFormatter.Converters
{
    public class HttpContentToFormDataConverter
    {
        public async Task<FormData> Convert(HttpContent content)
        {
            if(content == null)
                throw new ArgumentNullException("content");

            //commented to provide more details about incorrectly formatted data from ReadAsMultipartAsync method
            /*if (!content.IsMimeMultipartContent())
            {
                throw new Exception("Unsupported Media Type");
            }*/

            //http://stackoverflow.com/questions/15201255/request-content-readasmultipartasync-never-returns
            MultipartMemoryStreamProvider multipartProvider = null;
            await Task.Factory
                .StartNew(() => multipartProvider = content.ReadAsMultipartAsync().Result,
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning, // guarantees separate thread
                    TaskScheduler.Default);

            var multipartFormData = await Convert(multipartProvider);
            return multipartFormData;
        }

        public async Task<FormData> Convert(MultipartMemoryStreamProvider multipartProvider)
        {
            var multipartFormData = new FormData();

            foreach (var file in multipartProvider.Contents.Where(x => IsFile(x.Headers.ContentDisposition)))
            {
                var name = FixName(file.Headers.ContentDisposition.Name);
                string fileName = FixFilename(file.Headers.ContentDisposition.FileName);
                string mediaType = file.Headers.ContentType?.MediaType;

                using (var stream = await file.ReadAsStreamAsync())
                {
                    byte[] buffer = ReadAllBytes(stream);
                    if (buffer.Length >= 0)
                    {
                        multipartFormData.Add(name, new HttpFile(fileName, mediaType, buffer));
                    }
                }
            }

            foreach (var part in multipartProvider.Contents.Where(x => x.Headers.ContentDisposition.DispositionType == "form-data"
                                                                  && !IsFile(x.Headers.ContentDisposition)))
            {
                var name = FixName(part.Headers.ContentDisposition.Name);
                var data = await part.ReadAsStringAsync();
                multipartFormData.Add(name, data);
            }

            return multipartFormData;
        } 

        private bool IsFile(ContentDispositionHeaderValue disposition)
        {
            return !string.IsNullOrEmpty(disposition.FileName);
        }

        private static string FixName(string token)
        {
            var res = UnquoteToken(token);
            return NormalizeJQueryToMvc(res);
        }

        /// <summary>
        /// Remove bounding quotes on a token if present
        /// </summary>
        private static string UnquoteToken(string token)
        {
            if (String.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal) && token.Length > 1)
            {
                return token.Substring(1, token.Length - 2);
            }

            return token;
        }

        // This is a helper method to use Model Binding over a JQuery syntax. 
        // Normalize from JQuery to MVC keys. The model binding infrastructure uses MVC keys
        // x[] --> x
        // [] --> ""
        // x[field]  --> x.field, where field is not a number
        private static string NormalizeJQueryToMvc(string key)
        {
            if (key == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (true)
            {
                int indexOpen = key.IndexOf('[', i);
                if (indexOpen < 0)
                {
                    sb.Append(key, i, key.Length - i);
                    break; // no more brackets
                }

                sb.Append(key, i, indexOpen - i); // everything up to "["

                // Find closing bracket.
                int indexClose = key.IndexOf(']', indexOpen);
                if (indexClose == -1)
                {
                    throw new Exception($"Error find closing bracket in key \"{key}\"");
                }

                if (indexClose == indexOpen + 1)
                {
                    // Empty bracket. Signifies array. Just remove. 
                }
                else
                {
                    if (char.IsDigit(key[indexOpen + 1]))
                    {
                        // array index. Leave unchanged. 
                        sb.Append(key, indexOpen, indexClose - indexOpen + 1);
                    }
                    else
                    {
                        // Field name.  Convert to dot notation. 
                        sb.Append('.');
                        sb.Append(key, indexOpen + 1, indexClose - indexOpen - 1);
                    }
                }

                i = indexClose + 1;
                if (i >= key.Length)
                {
                    break; // end of string
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Amend filenames to remove surrounding quotes and remove path from IE
        /// </summary>
        private static string FixFilename(string originalFileName)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
                return string.Empty;

            var result = originalFileName.Trim();
            
            // remove leading and trailing quotes
            result = result.Trim('"');

            // remove full path versions
            if (result.Contains("\\"))
                result = Path.GetFileName(result);

            return result;
        }

        private byte[] ReadAllBytes(Stream input)
        {
            using (var stream = new MemoryStream())
            {
                input.CopyTo(stream);
                return stream.ToArray();
            }
        }
    }
}
