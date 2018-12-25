using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Tests
{
    public class RabbitMQServerFixture<TStartup> : IDisposable
          where TStartup : class
    {
        public InProcessTestServer<TStartup> FirstServer { get; private set; }
        public InProcessTestServer<TStartup> SecondServer { get; private set; }

        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDisposable _logToken;
        public RabbitMQServerFixture()
        {
            if (Docker.Default == null)
            {
                return;
            }

            var testLog = AssemblyTestLog.ForAssembly(typeof(RabbitMQServerFixture<TStartup>).Assembly);
            _logToken = testLog.StartTestLog(null, $"{nameof(RabbitMQServerFixture<TStartup>)}_{typeof(TStartup).Name}", out _loggerFactory, LogLevel.Trace, "RabbitMQServerFixture");
            _logger = _loggerFactory.CreateLogger<RabbitMQServerFixture<TStartup>>();

            Docker.Default.Start(_logger);

            FirstServer = StartServer();
            SecondServer = StartServer();
        }

        private InProcessTestServer<TStartup> StartServer()
        {
            try
            {
                return new InProcessTestServer<TStartup>(_loggerFactory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server failed to start.");
                throw;
            }
        }

        public void Dispose()
        {
            if (Docker.Default != null)
            {
                FirstServer.Dispose();
                SecondServer.Dispose();
                Docker.Default.Stop(_logger);
                _logToken.Dispose();
            }
        }
    }
}
