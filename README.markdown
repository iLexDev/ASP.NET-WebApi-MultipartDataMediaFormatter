ASP.NET WebApi MultipartDataMediaFormatter
=============

This is solution for automatic binding action parameters of custom types (including files) encoded as multipart/form-data. It works similar to ASP.NET MVC binding. This media type formatter can be used also for sending objects (using HttpClient) with automatic serialization to multipart/form-data.   

**This formatter can process:** 

* **custom non enumerable classes (deep nested classes supported)**       
* **all simple types that can be converted from/to string (using TypeConverter)** 
* **files (MultipartDataMediaFormatter.Infrastructure.HttpFile class)** 
* **generic arrays** 
* **generic lists** 
* **generic dictionaries** 

Using the code        
=================

Install formatter from Nuget:
```c#
Install-Package MultipartDataMediaFormatter.V2
```

Add it to WebApi formatters collection: 

if WebApi hosted on IIS (on Application Start):       

```c#
GlobalConfiguration.Configuration.Formatters.Add(new FormMultipartEncodedMediaTypeFormatter(new MultipartFormatterSettings()));    
```
if WebApi is self-hosted:  

```c#
new HttpSelfHostConfiguration(<url>).Formatters.Add(new FormMultipartEncodedMediaTypeFormatter(new MultipartFormatterSettings()));      
```  
Using formatter for sending objects (example from test project):    

```c#
private ApiResult<T> PostModel<T>(T model, string url)
{
 var mediaTypeFormatter = new FormMultipartEncodedMediaTypeFormatter(new MultipartFormatterSettings()
    {
        SerializeByteArrayAsHttpFile = true,
        CultureInfo = CultureInfo.CurrentCulture,
        ValidateNonNullableMissedProperty = true
    });
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
```
You can use MultipartDataMediaFormatter.Infrastructure.FormData class to access raw http data:  

```c#
[HttpPost]
public void PostFileBindRawFormData(MultipartDataMediaFormatter.Infrastructure.FormData formData)
  {
      HttpFile file;
      formData.TryGetValue(<key>, out file);
  }
```
Bind custom model example:

```c#
//model example
public class PersonModel
{
   public string FirstName {get; set;}
   public string LastName {get; set;}
   public DateTime? BirthDate {get; set;}
   public HttpFile AvatarImage {get; set;}
   public List<HttpFile> Attachments {get; set;}
   public List<PersonModel> ConnectedPersons {get; set;}
   public PersonModel Creator {get; set;}
   public List<string> Attributes {get; set;}
}

//api controller example
[HttpPost]
public void PostPerson(PersonModel model)
{
   //do something with the model
}

/*
Client http form keys:
* FirstName
* LastName
* BirthDate
* AvatarImage

* Attachments[0]
* Attachments[1]
* ... other Attachments[0...n]

* ConnectedPersons[0].FirstName
* ConnectedPersons[0].LastName
* ... other properties for ConnectedPersons[0] property

* Creator.FirstName
* Creator.LastName
* ... other properties for Creator property

* Attributes[0]
* Attributes[1]
* ... other Attributes[0...n]
 or you can use not indexed names for simple types:
 * Attributes
 * Attributes
 * ... other Attributes
*/
```

## History

##### Version 2.0.2 (2018-06-09)

* signed the project with a strong name (without password) to allow referencing this project in projects that were signed with a strong name

##### Version 2.0.1 (2018-02-14)

* added possibility of using IEnumerable<> and IDictionary<,> as types for model's properties, for example: ``` public IEnumerable<PersonModel> Persons {get;set;}```

##### Version 2.0.0 (2017-05-27)

* added Nuget package [MultipartDataMediaFormatter.V2](https://www.nuget.org/packages/MultipartDataMediaFormatter.V2)

##### Version 1.0.2 (2016-08-12)

* parsing lists of simple types and files with not indexed naming scheme (keys have same names like "propName" or "propName[]")
* parsing values "on" and "off" for boolean properties
* binding HttpFile from http request as byte array if model has such property
* added class ``` MultipartDataMediaFormatter.Infrastructure.MultipartFormatterSettings``` to control:
  * CultureInfo
  * serializing byte array as HttpFile when sending data
  * validating non nullable value types properties if there is no appropriate keys in http request

##### Version 1.0.1 (2014-04-03)
* fixed a bug that caused Exception (No MediaTypeFormatter is available to read an object of type <type name>) when posted data use multipart boundary different from used inside formatter code
* fixed a bug that caused error when binding model with recursive properties.

##### Version 1.0 (2013-11-22)
* First release

## Notes

For successfully running tests from the test project you should run Visual Studio with administrator rights because of using Self Hosted WebApi Server ```System.Web.Http.SelfHost.HttpSelfHostServer```

## License

Licensed under the [MIT License](http://www.opensource.org/licenses/mit-license.php).