# ASP.NET Core SignalR RabbitMQ Scaleout 

## About

maskx.AspNetCore.SignalR.RabbitMQ is an implementation of an HubLifetimeManager using RabbitMQ as the backing store. This allows a signalr web application to be scaled across a web farm.

## Usage

### Install

A compiled library is available via NuGet

To install via the nuget package console

```
Install-Package maskx.AspNetCore.SignalR.RabbitMQ
``` 

To install via the nuget user interface in Visual Studio the package to search for is "maskx.AspNetCore.SignalR.RabbitMQ"

### Configure

Modify Startup.cs

``` CSharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSignalR()
            .AddRabbitMQ(options =>
            {
                options.ConnectionFactory = new ConnectionFactory()
                {
                    HostName = "localhost",
                    Port = 5672
                };
                options.ExchangeName = "signalr-RabbitMQ";
            });
}
```

## License

The MIT License (MIT) - See file 'LICENSE' in this project

## Reference

* https://github.com/aspnet/AspNetCore/tree/master/src/SignalR/src/Microsoft.AspNetCore.SignalR.StackExchangeRedis
* https://github.com/aspnet/AspNetCore/tree/master/src/SignalR/test/Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests


