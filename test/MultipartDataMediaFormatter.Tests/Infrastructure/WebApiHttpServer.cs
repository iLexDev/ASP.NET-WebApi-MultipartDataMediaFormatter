using System;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace MultipartDataMediaFormatter.Tests.Infrastructure
{
    public class WebApiHttpServer : IDisposable
    {
        private HttpSelfHostServer Server;

        public WebApiHttpServer(string serverUrl, MediaTypeFormatter formatter)
        {
            var config = new HttpSelfHostConfiguration(serverUrl);
            config.Formatters.Add(formatter);
            config.Routes.MapHttpRoute(
                "API Default", "{controller}/{action}",
                new { id = RouteParameter.Optional });

            Server = new HttpSelfHostServer(config);
            Server.OpenAsync().Wait();
        }

        public void Dispose()
        {
            if (Server != null)
            {
                Server.CloseAsync().Wait();
                Server.Dispose();
                Server = null;
            }
        }
    }
}
