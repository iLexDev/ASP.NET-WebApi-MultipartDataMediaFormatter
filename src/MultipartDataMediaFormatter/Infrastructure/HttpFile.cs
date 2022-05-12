using System.IO;

namespace MultipartDataMediaFormatter.Infrastructure
{
    public class HttpFile
    {
        public string FileName { get; set; }
        public string MediaType { get; set; }
        public byte[] Buffer { get; set; }

        public HttpFile() { }

        public HttpFile(string fileName, string mediaType, byte[] buffer)
        {
            FileName = fileName;
            MediaType = mediaType;
            Buffer = buffer;
        }
        
        public void SaveAs(string fileName)
        {
            var fileStream = new FileStream(fileName, FileMode.Create);
            try
            {
                fileStream.Write(Buffer, 0, Buffer.Length);
                fileStream.Flush();
            }

            finally
            {
                fileStream.Close();
            }
        }
    }
}
