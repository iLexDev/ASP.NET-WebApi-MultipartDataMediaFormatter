using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultipartDataMediaFormatter.Infrastructure;
using MultipartDataMediaFormatter.Tests.Infrastructure;
using MultipartDataMediaFormatter.Tests.Models;

namespace MultipartDataMediaFormatter.Tests
{
    [TestClass]
    public class Tests
    {
        private const string BaseApiAddress = "http://localhost:8080";

        [TestInitialize]
        public void TestInit()
        {
            //need for correct comparing validation messages
            var enCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = enCulture;
            CultureInfo.DefaultThreadCurrentCulture = enCulture;
        }

        [TestMethod]
        public void TestComplexModelPost()
        {
            TestPost(PreparePersonModel(), "TestApi/PostPerson");
        }

        [TestMethod]
        public void TestModelWithoutPropertiesPost()
        {
            var personModel = new EmptyModel();
            TestPost(personModel, "TestApi/PostEmptyModel", "Cannot convert data to multipart/form-data format. No data found.");
        }

        [TestMethod]
        public void TestComplexModelWithValidationErrorsPost()
        {
            TestPost(PreparePersonModelWithValidationErrors(), "TestApi/PostPerson",
                "model.LastName: The LastName field is required. model.Photo: The Photo field is required. model.SomeGenericProperty.GenericValue: The GenericValue field is required.");
        }

        [TestMethod]
        public void TestComplexModelPostAndApiActionBindAsRawFormData()
        {
            TestPost(PreparePersonModel(), "TestApi/PostPersonBindRawFormData");
        }

        [TestMethod]
        public void TestFilePost()
        {
            TestPost(PrepareFileModel(), "TestApi/PostFile");
        }

        [TestMethod]
        public void TestFilePostAndApiActionBindAsRawFormData()
        {
            TestPost(PrepareFileModel(), "TestApi/PostFileBindRawFormData");
        }


        [TestMethod]
        public void TestStringPost()
        {
            TestPost(PrepareStringModel(), "TestApi/PostString");
        }

        [TestMethod]
        public void TestStringPostAndApiActionBindAsRawFormData()
        {
            TestPost(PrepareStringModel(), "TestApi/PostStringBindRawFormData");
        }


        [TestMethod]
        public void TestFormDataPost()
        {
            TestPost(PrepareFormDataModel(), "TestApi/PostFormData");
        }

        [TestMethod]
        public void TestPostWithoutFormatter()
        {
            PersonModel model;
            var httpContent = PreparePersonModelHttpContent(out model);

            var result = PostPersonModelHttpContent(httpContent);

            Assert.AreEqual("model.PersonId: The value is required. model.CreatedDateTime: The value is required.", result.ErrorMessage);

            AssertModelsEquals(model, result.Value);
        }

        [TestMethod]
        public void TestPostWithoutFormatterNotNullableValidationNotRequired()
        {
            PersonModel model;
            var httpContent = PreparePersonModelHttpContent(out model);

            var formatter = new FormMultipartEncodedMediaTypeFormatter(new MultipartFormatterSettings()
            {
                SerializeByteArrayAsHttpFile = true,
                CultureInfo = CultureInfo.CurrentCulture,
                ValidateNonNullableMissedProperty = false
            });

            var result = PostPersonModelHttpContent(httpContent, formatter);

            Assert.AreEqual(null, result.ErrorMessage);

            AssertModelsEquals(model, result.Value);
        }

        [TestMethod]
        public void TestPostWithoutFormatterSerializeByteArrayAsIndexedArray()
        {
            PersonModel model;
            var httpContent = PreparePersonModelHttpContent(out model);

            var formatter = new FormMultipartEncodedMediaTypeFormatter(new MultipartFormatterSettings()
            {
                SerializeByteArrayAsHttpFile = false,
                CultureInfo = CultureInfo.CurrentCulture,
                ValidateNonNullableMissedProperty = false
            });

            var result = PostPersonModelHttpContent(httpContent, formatter);

            Assert.AreEqual(null, result.ErrorMessage);

            AssertModelsEquals(model, result.Value);
        }

        private ApiResult<T> TestPost<T>(T model, string url, string errorMessage = null)
        {
            ApiResult<T> result = null;
            try
            {
                result = PostModel(model, url);
            }
            catch (Exception ex)
            {
                if (errorMessage != ex.GetBaseException().Message)
                {
                    throw;
                }
            }

            if (result != null)
            {
                if (String.IsNullOrWhiteSpace(errorMessage))
                {
                    Assert.IsTrue(String.IsNullOrWhiteSpace(result.ErrorMessage), result.ErrorMessage);
                    AssertModelsEquals(model, result.Value);
                }
                else
                {
                    Assert.AreEqual(errorMessage, result.ErrorMessage, "Invalid ErrorMessage");
                }
            }

            return result;
        }

        private void AssertModelsEquals(object originalModel, object returnedModel)
        {
            var compareObjects = new CompareLogic(new ComparisonConfig() {MaxDifferences = 10 });
            var comparisonResult = compareObjects.Compare(originalModel, returnedModel);
            Assert.IsTrue(comparisonResult.AreEqual, "Source model is not the same as returned model. {0}", comparisonResult.DifferencesString);
        }

        private ApiResult<T> PostModel<T>(T model, string url)
        {
            var mediaTypeFormatter = GetFormatter();

            using (new WebApiHttpServer(BaseApiAddress, mediaTypeFormatter))
            using (var client = CreateHttpClient(BaseApiAddress))
            using (HttpResponseMessage response = client.PostAsync(url, model, mediaTypeFormatter).Result)
            {
                ApiResult<T> resultModel;
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var error = response.Content.ReadAsAsync<List<ResponseErrorItem>>(new[] { mediaTypeFormatter }).Result;
                    var err = error.Where(m => m.Key == "ExceptionMessage").Select(m => m.Value).FirstOrDefault();
                    if (String.IsNullOrWhiteSpace(err))
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        Assert.Fail(responseContent);
                    }
                    resultModel = new ApiResult<T>()
                    {
                        ErrorMessage = err
                    };
                }
                else
                {
                    resultModel = response.Content.ReadAsAsync<ApiResult<T>>(new[] { mediaTypeFormatter }).Result;
                }
                return resultModel;
            }
        }

        private ApiResult<PersonModel> PostPersonModelHttpContent(HttpContent httpContent, MediaTypeFormatter mediaTypeFormatter = null)
        {
            mediaTypeFormatter = mediaTypeFormatter ?? GetFormatter();

            using (new WebApiHttpServer(BaseApiAddress, mediaTypeFormatter))
            using (var client = CreateHttpClient(BaseApiAddress))
            using (HttpResponseMessage response = client.PostAsync("TestApi/PostPerson", httpContent).Result)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var err = response.Content.ReadAsStringAsync().Result;
                    Assert.Fail(err);
                }
                var resultModel = response.Content.ReadAsAsync<ApiResult<PersonModel>>(new[] { mediaTypeFormatter }).Result;
                return resultModel;
            }
        }

        private PersonModel PreparePersonModel()
        {
            return new PersonModel
            {
                    PersonId = Guid.NewGuid(),
                    FirstName = "John",
                    LastName = "Doe",
                    RegisteredDateTime = DateTime.Now,
                    CreatedDateTime = DateTime.Now.AddDays(-10),
                    Age = 33,
                    Score = 150.7895m,
                    ActivityProgress = 25.4587f,
                    ScoreScaleFactor = 0.25879,
                    IsActive = true,
                    PersonType = PersonTypes.Admin,
                    Photo = new HttpFile("photo.png", "image/png", new byte[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }),
                    SomeGenericProperty = new SomeValue<PersonProperty>() { Name = "newname", GenericValue = new PersonProperty() { PropertyCode = 8, PropertyName = "addname",}},
                    Properties = new Dictionary<string, PersonProperty>
                    {
                        { "first", new PersonProperty { PropertyCode = 1, PropertyName = "Alabama", Bytes = new byte[] { 11, 3, 24, 23 }} },
                        { "second", new PersonProperty { PropertyCode = 2, PropertyName = "New York" } }
                    },
                    Roles = new List<PersonRole>
                    {
                        new PersonRole { RoleId = 1, RoleName = "admin" },
                        new PersonRole { RoleId = 2, RoleName = "user" }
                    },
                    Attachments = new List<HttpFile>
                    {
                        new HttpFile("photo2.png", "image/png", new byte[] { 4, 3, 24, 23  }),
                        new HttpFile("photo3.jpg", "image/jpg", new byte[] { 80, 31, 12, 3, 78, 45 })
                    },
                    ConnectedPersons = new List<PersonModel>()
                                       {
                                           new PersonModel()
                                           {
                                               FirstName = "inner first name",
                                               LastName = "inner last name",
                                               Photo = new HttpFile("photo.png", "image/png", new byte[] { 0, 1, 2, 3, 7 }),
                                               Attachments = new List<HttpFile>
                                               {
                                                    new HttpFile("photo21.png", "image/png", new byte[] { 4, 3, 24, 24  }),
                                               },
                                               IntProperties = new Dictionary<int, int>() { { 1, 2 } },
                                           },
                                           new PersonModel()
                                           {
                                               FirstName = "inner first name 2",
                                               LastName = "inner last name 2",
                                               Photo = new HttpFile("photo.png", "image/png", new byte[] { 0, 1, 2, 3, 7 }),
                                               Attachments = new List<HttpFile>
                                               {
                                                    new HttpFile("photo211.png", "image/png", new byte[] { 4, 3, 24, 25  }),
                                               },
                                               Bytes = new byte[] { 4, 3, 24, 23 },
                                               Ints = new List<int>() { 10 }
                                           }
                                       },
                    Ints = new List<int>() { 10 },
                    IntProperties = new Dictionary<int, int>() { { 1, 2 } },
                    Bytes = new byte[] { 4, 3, 24, 23 }
                };
        }

        private PersonModel PreparePersonModelWithValidationErrors()
        {
            return new PersonModel
            {
                FirstName = "John",
                SomeGenericProperty = new SomeValue<PersonProperty>() { Name = "newname" },
            };
        }

        private FormData PrepareFormDataModel()
        {
            var model = new FormData();
            model.Add("first", "111");
            model.Add("second", "string");
            model.Add("file1", new HttpFile("photo2.png", "image/png", new byte[] { 4, 3, 24, 23 }));
            model.Add("file2", new HttpFile("photo3.jpg", "image/jpg", new byte[] { 80, 31, 12, 3, 78, 45 }));

            return model;
        }

        private HttpFile PrepareFileModel()
        {
            return new HttpFile("testImage.png", "images/png", new byte[] {10, 45, 7});
        }

        private string PrepareStringModel()
        {
            return "some big text";
        }

        private HttpContent PreparePersonModelHttpContent(out PersonModel personModel)
        {
            personModel = new PersonModel()
            {
                FirstName = "First",
                LastName = "Last",
                Photo = new HttpFile("photo.png", "image/png", new byte[] { 0, 1, 2, 3, 7 }),
                Years = new List<int>()
                {
                    2001, 2010, 2015
                },
                Roles = new List<PersonRole>
                {
                    new PersonRole()
                    {   
                        RoleId = 1,
                        Rights = new List<int>(){ 1, 2, 5 }
                    }
                },
                IsActive = true,
                ActivityProgress = null,
                Attachments = new List<HttpFile>()
                {
                    new HttpFile("file1.tt", "text/plain", new byte[] { 1,3,5 }),
                    new HttpFile("file2.cf", "text/plain", new byte[] { 4,2,5 })
                }
            };

            var httpContent = new MultipartFormDataContent("testnewboundary");

            httpContent.Add(new StringContent(personModel.LastName), "LastName");
            httpContent.Add(new StringContent(personModel.FirstName), "FirstName");
            httpContent.Add(new StringContent(personModel.ActivityProgress == null ? "undefined" : personModel.ActivityProgress.ToString()), "ActivityProgress");
            httpContent.Add(new StringContent(personModel.IsActive ? "on" : "off"), "IsActive");

            foreach (var year in personModel.Years)
            {
                httpContent.Add(new StringContent(year.ToString()), "Years");    
            }

            httpContent.Add(new StringContent(personModel.Roles[0].RoleId.ToString()), "Roles[0].RoleId");    
            foreach (var right in personModel.Roles[0].Rights)
            {
                httpContent.Add(new StringContent(right.ToString()), "Roles[0].Rights");    
            }

            var fileContent = new ByteArrayContent(personModel.Photo.Buffer);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(personModel.Photo.MediaType);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = personModel.Photo.FileName,
                Name = "Photo"
            };
            httpContent.Add(fileContent);

            for (int i = 0; i < personModel.Attachments.Count; i++)
            {
                var attachment = personModel.Attachments[i];
                var content = new ByteArrayContent(attachment.Buffer);
                content.Headers.ContentType = new MediaTypeHeaderValue(attachment.MediaType);
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = attachment.FileName,
                    Name = "Attachments"
                };
                httpContent.Add(content);
            }

            return httpContent;
        }

        private MediaTypeFormatter GetFormatter()
        {
            return new FormMultipartEncodedMediaTypeFormatter(new MultipartFormatterSettings()
            {
                SerializeByteArrayAsHttpFile = true,
                CultureInfo = CultureInfo.CurrentCulture,
                ValidateNonNullableMissedProperty = true
            });
        }
        private HttpClient CreateHttpClient(string baseUrl)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl)
            };
            return client;
        }
    }
}
