using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            }).AddMessagePackProtocol()
            .AddRabbitMQ(options =>
                {
                    options.ConnectionFactory = new ConnectionFactory()
                    {
                        HostName = "localhost",
                        Port = 5672
                    };
                    options.ExchangeName = "signalr-RabbitMQ";
                });

            services.AddSingleton<IUserIdProvider, UserNameIdProvider>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSignalR(options => options.MapHub<EchoHub>("/echo"));
        }

        private class UserNameIdProvider : IUserIdProvider
        {
            public string GetUserId(HubConnectionContext connection)
            {
                // This is an AWFUL way to authenticate users! We're just using it for test purposes.
                var userNameHeader = connection.GetHttpContext().Request.Headers["UserName"];
                if (!StringValues.IsNullOrEmpty(userNameHeader))
                {
                    return userNameHeader;
                }

                return null;
            }
        }
    }
}
