using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NWheels.Kernel.Api.Extensions;
using NWheels.Kernel.Api.Injection;
using NWheels.Microservices.Api;
using NWheels.Communication.Api.Http;
using System.Linq;
using System.IO;

namespace NWheels.Communication.Api.Extensions
{
    public static class BuilderExtensions
    {
        public static void UseHttpEndpoint(
            this MicroserviceHostBuilder builder, 
            string name = "Default", 
            Action<HttpEndpointConfigurationBuilder> configure = null)
        {
            builder.ContributeComponents((exitingComponents, newComponents) => {
                newComponents.ContributeHttpEndpoint(name, configure);
            });
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        
        public static void ContributeHttpEndpoint(
            this IComponentContainerBuilder builder, 
            string name = "Default", 
            Action<HttpEndpointConfigurationBuilder> configure = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Endpoint name must be a non-empty string", nameof(name));
            }

            HttpEndpointAdapterInjectionPort.ConfiguratorAction configurator = 
                (config, exisgingComponents, newComponents) => {
                    var configBuilder = new HttpEndpointConfigurationBuilder(newComponents, config);
                    configure(configBuilder);
                };

            var adapterPort = new HttpEndpointAdapterInjectionPort(builder, configurator); 
            builder.RegisterAdapterPort(adapterPort);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public class HttpEndpointConfigurationBuilder
        {
            private readonly IComponentContainerBuilder _containerBuilder;
            private readonly IHttpEndpointConfigElement _configElement;

            //-------------------------------------------------------------------------------------------------------------------------------------------------
            
            public HttpEndpointConfigurationBuilder(IComponentContainerBuilder containerBuilder, IHttpEndpointConfigElement configElement)
            {
                _containerBuilder = containerBuilder;
                _configElement = configElement;
            }

            //-------------------------------------------------------------------------------------------------------------------------------------------------

            public HttpEndpointConfigurationBuilder ListenPort(int number)
            {
                _configElement.Port = number;
                return this;
            }

            //-------------------------------------------------------------------------------------------------------------------------------------------------

            public HttpEndpointConfigurationBuilder StaticFolder(
                string requestPath, 
                string[] localPath, 
                string[] defaultFiles = null, 
                string cacheControl = null,
                string defaultContentType = null,
                bool browasble = false)
            {
                var item = _configElement.StaticFolders.NewItem();

                item.RequestBasePath = requestPath;
                item.LocalRootPath = Path.Combine(localPath.Select(p => p.ToPathString()).ToArray());
                item.DefaultFiles = defaultFiles;
                item.DefaultContentType = defaultContentType;
                item.CacheControl = cacheControl;
                item.EnableDirectoryBrowsing = browasble;

                _configElement.StaticFolders.Add(item);
                return this;
            }

            //-------------------------------------------------------------------------------------------------------------------------------------------------

            public HttpEndpointConfigurationBuilder Middleware<TMiddleware>()
                where TMiddleware : ICommunicationMiddleware<HttpContext>
            {
                _containerBuilder.RegisterComponentType<TMiddleware>();
                _configElement.MiddlewarePipeline.Add(typeof(TMiddleware));

                return this;
            }
        }
    }
}
