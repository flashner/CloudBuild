﻿namespace CloudBuildWeb
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Net.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class CloudBuildWeb : StatelessService
    {
        public CloudBuildWeb(StatelessServiceContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(
                    serviceContext =>
                        new KestrelCommunicationListener(
                            serviceContext,
                            "ServiceEndpoint",
                            (url, listener) =>
                            {
                                ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                                return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<HttpClient>(new HttpClient())
                                            .AddSingleton<FabricClient>(new FabricClient())
                                            .AddSingleton<StatelessServiceContext>(serviceContext))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url)
                                    .Build();
                            }))
            };
        }

        /// <summary>
        /// Constructs a service name for a specific poll.
        /// Example: fabric:/CloudBuildApplication/polls/name-of-poll
        /// </summary>
        /// <param name="poll"></param>
        /// <returns></returns>
        internal static Uri GetCloudBuildDataServiceName(ServiceContext context)
        {
            return new Uri($"{context.CodePackageActivationContext.ApplicationName}/CloudBuildData");
        }
    }
}