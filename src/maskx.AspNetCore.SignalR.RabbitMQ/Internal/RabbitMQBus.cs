﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Internal
{
    internal class RabbitMQBus : IDisposable
    {
        private RabbitMQOptions _RabbitMQOptions;
        private IConnection _Connection;
        private IModel _PublishModel;
        private IModel _SubscribeModel;
        private string _QueueName { get; }
        private readonly ILogger _Logger;
        private readonly RabbitMQProtocol _Protocol;
        private readonly SemaphoreSlim _Lock = new SemaphoreSlim(1, 1);
        private int _InternalId;
        private readonly AckHandler _ackHandler;
        private readonly ConcurrentDictionary<string, HubConnectionStore> _Groups = new ConcurrentDictionary<string, HubConnectionStore>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, HubConnectionContext> _Users = new ConcurrentDictionary<string, HubConnectionContext>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, HubConnectionContext> _Connections = new ConcurrentDictionary<string, HubConnectionContext>();

        public bool IsConnected
        {
            get
            {
                return _Connection != null && _Connection.IsOpen
                    && _PublishModel != null && _PublishModel.IsOpen
                    && _SubscribeModel != null && _SubscribeModel.IsOpen;
            }
        }
        public RabbitMQBus(RabbitMQOptions rabbitMQOptions, RabbitMQProtocol protocol, ILogger<RabbitMQBus> logger, string queueName)
        {
            this._Logger = logger;
            this._RabbitMQOptions = rabbitMQOptions;
            this._Protocol = protocol;
            this._QueueName = queueName;
            this._ackHandler = new AckHandler();
        }
        public async Task Connect()
        {
            await Task.Run(() =>
            {
                try
                {
                    this._Connection = _RabbitMQOptions.ConnectionFactory.CreateConnection();

                    this._PublishModel = this._Connection.CreateModel();
                    this._SubscribeModel = this._Connection.CreateModel();

                    this._PublishModel.ExchangeDeclare(this._RabbitMQOptions.ExchangeName, ExchangeType.Fanout, durable: true);
                    this._SubscribeModel.QueueDeclare(this._QueueName, durable: false, exclusive: false, autoDelete: true, arguments: new Dictionary<string, object>());
                    this._SubscribeModel.QueueBind(this._QueueName, this._RabbitMQOptions.ExchangeName, string.Empty);

                    var consumer = new EventingBasicConsumer(this._SubscribeModel);
                    consumer.Received += Consumer_ReceivedAsync;
                    this._SubscribeModel.BasicConsume(this._QueueName, autoAck: false, consumer);

                }
                catch (Exception ex)
                {
                    RabbitMQLog.ConnectionFailed(this._Logger, ex);
                }
            });
        }

        private void Consumer_ReceivedAsync(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var headers = e.BasicProperties.Headers;
                headers.TryGetValue("channelId", out object channelId);
                if (headers.TryGetValue("channel", out object channel))
                {
                    switch (Enum.Parse<RabbitMQChannel>(Encoding.UTF8.GetString((byte[])channel)))
                    {
                        case RabbitMQChannel.All:
                            Write(_Connections.Values.GetEnumerator(), _Protocol.ReadInvocation(e.Body), _Connections.Count);
                            break;
                        case RabbitMQChannel.Group:
                            WriteGroup(_Protocol.ReadInvocation(e.Body), Encoding.UTF8.GetString((byte[])channelId));
                            break;
                        case RabbitMQChannel.Groups:
                            var groups = _Protocol.ReadList((byte[])channelId);
                            var groupInvocation = _Protocol.ReadInvocation(e.Body);
                            foreach (var group in groups)
                            {
                                WriteGroup(groupInvocation, group);
                            }
                            break;
                        case RabbitMQChannel.Connection:
                            WriteConnection(_Protocol.ReadInvocation(e.Body), Encoding.UTF8.GetString((byte[])channelId));
                            break;
                        case RabbitMQChannel.Connections:
                            var connections = _Protocol.ReadList((byte[])channelId);
                            var connectionInvocation = _Protocol.ReadInvocation(e.Body);
                            foreach (var connection in connections)
                            {
                                WriteConnection(connectionInvocation, connection);
                            }
                            break;
                        case RabbitMQChannel.User:
                            WriteUser(_Protocol.ReadInvocation(e.Body), Encoding.UTF8.GetString((byte[])channelId));
                            break;
                        case RabbitMQChannel.Users:
                            var users = _Protocol.ReadList((byte[])channelId);
                            var userInvocation = _Protocol.ReadInvocation(e.Body);
                            foreach (var user in users)
                            {
                                WriteUser(userInvocation, user);
                            }
                            break;
                        case RabbitMQChannel.GroupCommand:
                            if (this._Connections.TryGetValue(channelId.ToString(), out HubConnectionContext cmdConnection))
                            {
                                var commandInvocation = _Protocol.ReadGroupCommand(e.Body);
                                switch (commandInvocation.Action)
                                {
                                    case GroupAction.Add:
                                        AddGroup(cmdConnection, commandInvocation.GroupName).Wait();
                                        break;
                                    case GroupAction.Remove:
                                        RemoveGroup(cmdConnection, commandInvocation.GroupName).Wait();
                                        break;
                                    default:
                                        break;
                                }
                                SendAck(commandInvocation.Id, commandInvocation.ServerName).Wait();
                            }
                            break;
                        case RabbitMQChannel.Ack:
                            if (channelId.ToString() == this._QueueName)
                            {
                                var ackId = _Protocol.ReadAck(e.Body);
                                _ackHandler.TriggerAck(ackId);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            finally
            {
                this._SubscribeModel.BasicAck(e.DeliveryTag, multiple: false);
            }
        }

        #region publish to RabbitMQ
        private async Task SendAck(int messageId, string queueName)
        {
            await Task.Run(() =>
            {
                var message = _Protocol.WriteAck(messageId);

                var properties = new BasicProperties
                {
                    Headers = new Dictionary<string, object>
                    {
                        { "channel",RabbitMQChannel.Ack},
                        { "channelId", queueName}
                    }
                };
                _PublishModel.BasicPublish(_RabbitMQOptions.ExchangeName, "", properties, message);
            });
        }
        public async Task SendGroupManagement(RabbitMQGroupCommand command)
        {
            await Task.Run(() =>
            {
                var message = _Protocol.WriteGroupCommand(command);
                var properties = new BasicProperties
                {
                    Headers = new Dictionary<string, object>
                    {
                        { "channel",RabbitMQChannel.GroupCommand},
                        {"channelId",command.ConnectionId }
                    }
                };
                _PublishModel.BasicPublish(_RabbitMQOptions.ExchangeName, "", properties, message);

            });
        }
        public async Task Send(RabbitMQChannel channel, object channelId, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds = null)
        {
            await Task.Run(() =>
            {
                var list = channelId as IReadOnlyList<string>;
               if(list!=null)
                {
                    channelId = _Protocol.WriteList(list);
                }
                var message = _Protocol.WriteInvocation(methodName, args, excludedConnectionIds);
                var properties = new BasicProperties
                {
                    Headers = new Dictionary<string, object>
                    {
                        { "channel",channel.ToString()},
                        { "channelId",channelId }
                    }
                };
                _PublishModel.BasicPublish(_RabbitMQOptions.ExchangeName, "", properties, message);
            });
        }
        #endregion

        #region Subscribe
        public Task SubscribeToConnection(HubConnectionContext connection)
        {
            return Task.Run(() =>
            {
                _Connections.TryAdd(connection.ConnectionId, connection);
                RabbitMQLog.Subscribing(_Logger, "SubscribeToConnection:" + connection.ConnectionId);
            });

        }
        public Task UnSubscribeConnection(HubConnectionContext connection)
        {
            return Task.Run(() =>
            {
                _Connections.TryRemove(connection.ConnectionId, out HubConnectionContext value);
                RabbitMQLog.Unsubscribe(_Logger, "UnSubscribeConnection:" + connection.ConnectionId);
            });

        }
        public Task SubscribeToUser(HubConnectionContext connection)
        {
            return Task.Run(() =>
            {
                _Users.TryAdd(connection.UserIdentifier, connection);
                RabbitMQLog.Subscribing(_Logger, "SubscribeToUser:" + connection.ConnectionId);
            });
        }
        public Task UnSubscribeUser(HubConnectionContext connection)
        {

            return Task.Run(() =>
            {
                _Users.TryRemove(connection.UserIdentifier, out HubConnectionContext value);
                RabbitMQLog.Unsubscribe(_Logger, "UnSubscribeUser:" + connection.ConnectionId);
            });

        }
        #endregion

        #region group command
        private async Task AddGroup(HubConnectionContext connection, string group)
        {
            await _Lock.WaitAsync();
            try
            {
                HubConnectionStore subscription = this._Groups.GetOrAdd(group, _ => new HubConnectionStore());
                subscription.Add(connection);
            }
            finally
            {
                _Lock.Release();
            }
        }
        public async Task AddGroup(string connectionId, string group)
        {
            if (this._Connections.TryGetValue(connectionId, out HubConnectionContext connection))
                await AddGroup(connection, group);
            else
                await SendGroupActionAndWaitForAck(connectionId, group, GroupAction.Add);
        }
        public async Task RemoveGroup(HubConnectionContext connection, string group)
        {
            await _Lock.WaitAsync();
            try
            {
                if (_Groups.TryGetValue(group, out var subscription))
                {
                    subscription.Remove(connection);
                }
            }
            finally
            {
                _Lock.Release();
            }
        }
        public async Task RemoveGroup(string connectionId, string group)
        {
            if (_Connections.TryGetValue(connectionId, out HubConnectionContext connection))
                await RemoveGroup(connection, group);
            else
                await SendGroupActionAndWaitForAck(connectionId, group, GroupAction.Remove);
        }
        private async Task SendGroupActionAndWaitForAck(string connectionId, string groupName, GroupAction action)
        {
            var id = Interlocked.Increment(ref _InternalId);
            var ack = _ackHandler.CreateAck(id);

            RabbitMQGroupCommand command = new RabbitMQGroupCommand(id, action, this._QueueName, groupName, connectionId);
            await SendGroupManagement(command);

            await ack;
        }
        #endregion

        public void UnsubscribeAll()
        {
            this._Connections.Clear();
            this._Users.Clear();
            this._Groups.Clear();
        }
        public void Dispose()
        {
            UnsubscribeAll();
            _PublishModel?.Close();
            _SubscribeModel?.Close();
            _Connection?.Close();
        }

        #region Write HubConnectionContext
        private void WriteUser(RabbitMQInvocation invocation, string userId)
        {
            if (this._Users.TryGetValue(userId, out HubConnectionContext connection))
            {
                Write(connection, invocation);
            }
        }
        private void WriteConnection(RabbitMQInvocation invocation, string connectionId)
        {
            if (this._Connections.TryGetValue(connectionId, out HubConnectionContext hubConnection))
            {
                Write(hubConnection, invocation);
            }
        }

        private void WriteGroup(RabbitMQInvocation invocation, string groupId)
        {
            if (this._Groups.TryGetValue(groupId, out HubConnectionStore groupStroe))
            {
                Write(groupStroe.GetEnumerator(), invocation, groupStroe.Count);
            }
        }
        private void Write(HubConnectionContext connection, RabbitMQInvocation invocation)
        {
            connection.WriteAsync(invocation.Message);
        }
        private void Write(IEnumerator<HubConnectionContext> connections, RabbitMQInvocation invocation, int count = 1)
        {
            var tasks = new List<Task>(count);
            while (connections.MoveNext())
            {
                if (invocation.ExcludedConnectionIds == null || !invocation.ExcludedConnectionIds.Contains(connections.Current.ConnectionId))
                {
                    tasks.Add(connections.Current.WriteAsync(invocation.Message).AsTask());
                }
            }
            Task.WaitAll(tasks.ToArray());
        }
        #endregion
    }
}
