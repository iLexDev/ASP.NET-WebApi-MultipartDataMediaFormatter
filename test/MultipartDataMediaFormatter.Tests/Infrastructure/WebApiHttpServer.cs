using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using Microsoft.Owin.Testing;
using Owin;

namespace MultipartDataMediaFormatter.Tests.Infrastructure
{
    public class WebApiHttpServer : IDisposable
    {
        private readonly TestServer Server;

        public WebApiHttpServer(MediaTypeFormatter formatter)
        {
            Server = TestServer.Create(builder =>
            {
                var config = new HttpConfiguration();

                config.Formatters.Clear();
                config.Formatters.Add(formatter);
                config.Routes.MapHttpRoute(
                    "API Default", "{controller}/{action}",
                    new { id = RouteParameter.Optional });
                
                builder.UseWebApi(config);
            });
        }

        public HttpClient CreateClient()
        {
            return Server.HttpClient;
        }


        public void Dispose()
        {
            Server.Dispose();
        }
    }
}
