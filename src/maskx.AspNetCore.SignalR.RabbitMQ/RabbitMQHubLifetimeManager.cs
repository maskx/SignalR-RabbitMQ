using maskx.AspNetCore.SignalR.RabbitMQ.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.AspNetCore.SignalR.RabbitMQ
{
    public class RabbitMQHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable where THub : Hub
    {
        private RabbitMQBus _bus;
        private readonly ILogger _logger;
        private readonly string _serverName = GenerateServerName();
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1);

        public RabbitMQHubLifetimeManager(ILoggerFactory loggerFactory,
                                      ILogger<RabbitMQHubLifetimeManager<THub>> logger,
                                      IOptions<RabbitMQOptions> options,
                                      IHubProtocolResolver hubProtocolResolver)
        {
            _logger = logger;
            _bus = new RabbitMQBus(options.Value,
                new RabbitMQProtocol(hubProtocolResolver.AllProtocols),
                loggerFactory.CreateLogger<RabbitMQBus>(),
                _serverName);
            _ = EnsureRabbitMQServerConnection();
        }
        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            await EnsureRabbitMQServerConnection();
            var feature = new RabbitMQFeature();
            connection.Features.Set<IRabbitMQFeature>(feature);

            var connectionTask = Task.CompletedTask;
            var userTask = Task.CompletedTask;

            connectionTask = _bus.SubscribeToConnection(connection);

            if (!string.IsNullOrEmpty(connection.UserIdentifier))
            {
                userTask = _bus.SubscribeToUser(connection);
            }

            await Task.WhenAll(connectionTask, userTask);
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            var tasks = new List<Task>();
            tasks.Add(_bus.UnSubscribeConnection(connection));

            var feature = connection.Features.Get<IRabbitMQFeature>();
            var groupNames = feature.Groups;
            if (groupNames != null)
            {
                foreach (var group in groupNames.ToArray())
                {
                    groupNames.Remove(group);
                    tasks.Add(_bus.RemoveGroup(connection, group));
                }
            }

            if (!string.IsNullOrEmpty(connection.UserIdentifier))
            {
                tasks.Add(_bus.UnSubscribeUser(connection));
            }

            return Task.WhenAll(tasks);
        }
        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }
            return _bus.AddGroup(connectionId, groupName);
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            return _bus.RemoveGroup(connectionId, groupName);
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bus.Send(RabbitMQChannel.All, null, methodName, args);
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bus.Send(RabbitMQChannel.All, null, methodName, args, excludedConnectionIds);
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bus.Send(RabbitMQChannel.Connection, connectionId, methodName, args);
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bus.Send(RabbitMQChannel.Connections, connectionIds, methodName, args);

        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }
            return _bus.Send(RabbitMQChannel.Group, groupName, methodName, args);
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }
            return _bus.Send(RabbitMQChannel.Group, groupName, methodName, args, excludedConnectionIds);
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bus.Send(RabbitMQChannel.Groups, groupNames, methodName, args);
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bus.Send(RabbitMQChannel.User, userId, methodName, args);
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _bus.Send(RabbitMQChannel.Users, userIds, methodName, args);
        }
        public void Dispose()
        {
            _bus?.Dispose();
        }
        private static string GenerateServerName()
        {
            // Use the machine name for convenient diagnostics, but add a guid to make it unique.
            // Example: MyServerName_02db60e5fab243b890a847fa5c4dcb29
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }
        private async Task EnsureRabbitMQServerConnection()
        {
            if (!_bus.IsConnected)
            {
                await _connectionLock.WaitAsync();
                try
                {
                    if (!_bus.IsConnected)
                    {
                        await _bus.Connect();
                        if (_bus.IsConnected)
                        {
                            RabbitMQLog.Connected(_logger);
                        }
                        else
                        {
                            RabbitMQLog.NotConnected(_logger);
                        }
                    }
                }
                finally
                {
                    _connectionLock.Release();
                }
            }
        }
        private class LoggerTextWriter : TextWriter
        {
            private readonly ILogger _logger;

            public LoggerTextWriter(ILogger logger)
            {
                _logger = logger;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {

            }

            public override void WriteLine(string value)
            {
                RabbitMQLog.ConnectionMessage(_logger, value);
            }
        }

        private interface IRabbitMQFeature
        {
            HashSet<string> Groups { get; }
        }

        private class RabbitMQFeature : IRabbitMQFeature
        {
            public HashSet<string> Groups { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

}
