using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
            var enCulture = System.Globalization.CultureInfo.GetCultureInfo( "en-US" );
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = enCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = enCulture;        
        }        

        [TestMethod]
        public void TestComplexModelPost()
        {
            TestPost(PreparePersonModel(), "TestApi/PostPerson");
        }

        [TestMethod]
        public void TestComplexModelWithValidationErrorsPost()
        {
            TestPost(PreparePersonModelWithValidationErrors(), "TestApi/PostPerson", "The LastName field is required. The Photo field is required. The GenericValue field is required.");
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

        private ApiResult<T> TestPost<T>(T model, string url, string errorMessage = null)
        {
            var result = PostModel(model, url);

            if (String.IsNullOrWhiteSpace(errorMessage))
            {
                Assert.IsTrue(String.IsNullOrWhiteSpace(result.ErrorMessage), result.ErrorMessage);
            }
            else
            {
                Assert.AreEqual(errorMessage, result.ErrorMessage, "Invalid ErrorMessage");
            }
            
            AssertModelsEquals(model, result.Value);

            return result;
        }

        private void AssertModelsEquals(object originalModel, object returnedModel)
        {
            var compareObjects = new CompareObjects {MaxDifferences = 10 };
            Assert.IsTrue(compareObjects.Compare(originalModel, returnedModel), "Source model is not the same as returned model. {0}", compareObjects.DifferencesString);
        }

        private ApiResult<T> PostModel<T>(T model, string url)
        {
            var mediaTypeFormatter = new FormMultipartEncodedMediaTypeFormatter();

            using (new WebApiHttpServer(BaseApiAddress, mediaTypeFormatter))
            using (var client = CreateHttpClient(BaseApiAddress))
            using (HttpResponseMessage response = client.PostAsync(url, model, mediaTypeFormatter).Result)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var err = response.Content.ReadAsStringAsync().Result;
                    Assert.Fail(err);
                }
                var resultModel = response.Content.ReadAsAsync<ApiResult<T>>(new[] { mediaTypeFormatter }).Result;
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
                    Photo = new HttpFile("photo.png", "image/png", new byte[] { 0, 1, 2, 3, 7 }),
                    SomeGenericProperty = new SomeValue<PersonProperty>() { Name = "newname", GenericValue = new PersonProperty() { PropertyCode = 8, PropertyName = "addname",}},
                    Properties = new Dictionary<string, PersonProperty>
                    {
                        { "first", new PersonProperty { PropertyCode = 1, PropertyName = "Alabama" } },
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
                    }
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
