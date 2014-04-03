ASP.NET WebApi MultipartDataMediaFormatter
=============

This is solution for automatic binding action parameters of custom types (including files) encoded as multipart/form-data. It works similar to ASP.NET MVC binding. This media type formatter can be used also for sending objects (using HttpClient) with automatic serialization to multipart/form-data.   

**This formatter can process:** 

* **custom non enumarable classes (deep nested classes supported)**       
* **all simple types that can be converted from/to string (using TypeConverter)** 
* **files (MultipartDataMediaFormatter.Infrastructure.HttpFile class)** 
* **generic arrays** 
* **generic lists** 
* **generic dictionaries** 

Using the code        
=================

For using this formatter all you need is to simply add current formatter to WebApi formatters collection: 

if WebApi hosted on IIS (on Application Start):       

```c#
GlobalConfiguration.Configuration.Formatters.Add(new FormMultipartEncodedMediaTypeFormatter());    
```
if WebApi is self-hosted:  

```c#
new HttpSelfHostConfiguration(<url>).Formatters.Add(new FormMultipartEncodedMediaTypeFormatter());      
```  
Using formatter for sending objects (example from test project):    

```c#
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
}

//api controller example
[HttpPost]
public void PostPerson(PersonModel model)
{
   //do something with the model
}
```
## License

Licensed under the [MIT License](http://www.opensource.org/licenses/mit-license.php).