using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Internal
{
   internal class RabbitMQLog
    {
        private static readonly Action<ILogger, string, string, Exception> _connectingToEndpoints =
         LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1, "ConnectingToEndpoints"), "Connecting to RabbitMQ endpoints: {Endpoints}. Using Server Name: {ServerName}");

        private static readonly Action<ILogger, Exception> _connected =
            LoggerMessage.Define(LogLevel.Information, new EventId(2, "Connected"), "Connected to RabbitMQ.");

        private static readonly Action<ILogger, string, Exception> _subscribing =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(3, "Subscribing"), "Subscribing to channel: {Channel}.");

        private static readonly Action<ILogger, string, Exception> _receivedFromChannel =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(4, "ReceivedFromChannel"), "Received message from RabbitMQ channel {Channel}.");

        private static readonly Action<ILogger, string, Exception> _publishToChannel =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, "PublishToChannel"), "Publishing message to RabbitMQ channel {Channel}.");

        private static readonly Action<ILogger, string, Exception> _unsubscribe =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(6, "Unsubscribe"), "Unsubscribing from channel: {Channel}.");

        private static readonly Action<ILogger, Exception> _notConnected =
            LoggerMessage.Define(LogLevel.Error, new EventId(7, "Connected"), "Not connected to RabbitMQ.");

        private static readonly Action<ILogger, Exception> _connectionRestored =
            LoggerMessage.Define(LogLevel.Information, new EventId(8, "ConnectionRestored"), "Connection to RabbitMQ restored.");

        private static readonly Action<ILogger, Exception> _connectionFailed =
            LoggerMessage.Define(LogLevel.Error, new EventId(9, "ConnectionFailed"), "Connection to RabbitMQ failed.");

        private static readonly Action<ILogger, Exception> _failedWritingMessage =
            LoggerMessage.Define(LogLevel.Warning, new EventId(10, "FailedWritingMessage"), "Failed writing message.");

        private static readonly Action<ILogger, Exception> _internalMessageFailed =
            LoggerMessage.Define(LogLevel.Warning, new EventId(11, "InternalMessageFailed"), "Error processing message for internal server message.");

        public static void ConnectingToEndpoints(ILogger logger, AmqpTcpEndpoint endpoint, string serverName)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                if (endpoint != null)
                {
                    _connectingToEndpoints(logger, endpoint.ToString(),serverName,null);
                }
            }
        }

        public static void Connected(ILogger logger)
        {
            _connected(logger, null);
        }

        public static void Subscribing(ILogger logger, string channelName)
        {
            _subscribing(logger, channelName, null);
        }

        public static void ReceivedFromChannel(ILogger logger, string channelName)
        {
            _receivedFromChannel(logger, channelName, null);
        }

        public static void PublishToChannel(ILogger logger, string channelName)
        {
            _publishToChannel(logger, channelName, null);
        }

        public static void Unsubscribe(ILogger logger, string channelName)
        {
            _unsubscribe(logger, channelName, null);
        }

        public static void NotConnected(ILogger logger)
        {
            _notConnected(logger, null);
        }

        public static void ConnectionRestored(ILogger logger)
        {
            _connectionRestored(logger, null);
        }

        public static void ConnectionFailed(ILogger logger, Exception exception)
        {
            _connectionFailed(logger, exception);
        }

        public static void FailedWritingMessage(ILogger logger, Exception exception)
        {
            _failedWritingMessage(logger, exception);
        }

        public static void InternalMessageFailed(ILogger logger, Exception exception)
        {
            _internalMessageFailed(logger, exception);
        }
    }
}
