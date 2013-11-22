using System;
using System.Linq;
using System.Web.Http;
using MultipartDataMediaFormatter.Converters;
using MultipartDataMediaFormatter.Infrastructure;
using MultipartDataMediaFormatter.Infrastructure.Logger;
using MultipartDataMediaFormatter.Tests.Models;

namespace MultipartDataMediaFormatter.Tests.Controllers
{
    public class TestApiController : ApiController
    {
        [HttpPost]
        public ApiResult<PersonModel> PostPerson(PersonModel model)
        {
            return GetApiResult(model);
        }

        [HttpPost]
        public ApiResult<PersonModel> PostPersonBindRawFormData(FormData formData)
        {
            var logger = new FormDataConverterLogger();
            var dataToObjectConverter = new FormDataToObjectConverter(formData, logger);

            var person = (PersonModel)dataToObjectConverter.Convert(typeof(PersonModel));
            logger.EnsureNoErrors();

            return GetApiResult(person);
        }

        [HttpPost]
        public ApiResult<HttpFile> PostFile(HttpFile file)
        {
            return GetApiResult(file);
        }

        [HttpPost]
        public ApiResult<HttpFile> PostFileBindRawFormData(FormData formData)
        {
            HttpFile file;
            formData.TryGetValue("", out file);
            return GetApiResult(file);
        }

        [HttpPost]
        public ApiResult<string> PostString([FromBody] string data)
        {
            return GetApiResult(data);
        }

        [HttpPost]
        public ApiResult<string> PostStringBindRawFormData(FormData formData)
        {
            string data;
            formData.TryGetValue("", out data);
            return GetApiResult(data);
        }

        [HttpPost]
        public ApiResult<FormData> PostFormData(FormData formData)
        {
            return GetApiResult(formData);
        }

        private ApiResult<T> GetApiResult<T>(T value)
        {
            var result = new ApiResult<T>() { Value = value };
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .SelectMany(m => m.Value.Errors)
                    .Select(m => (m.ErrorMessage ?? (m.Exception != null ? m.Exception.Message : "")))
                    .ToList();

                result.ErrorMessage = String.Join(" ", errors);
            }
            return result;
        }
    }
}
