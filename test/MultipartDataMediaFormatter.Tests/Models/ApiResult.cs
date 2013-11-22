using MultipartDataMediaFormatter.Infrastructure;

namespace MultipartDataMediaFormatter.Tests.Models
{
    public class ApiResult<T>
    {
        public string ErrorMessage { get; set; }
        public T Value { get; set; }
    }
}
