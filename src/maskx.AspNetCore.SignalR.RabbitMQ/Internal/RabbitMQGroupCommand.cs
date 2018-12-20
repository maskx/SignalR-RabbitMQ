using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Internal
{
    public readonly struct RabbitMQGroupCommand
    {
        public int Id { get; }
        public GroupAction Action { get; }
        public string GroupName { get; }
        public string ConnectionId { get; }
        public string ServerName { get; }
        public RabbitMQGroupCommand(int id, GroupAction action,string serverName, string groupName, string connectionId)
        {
            Id = id;
            Action = action;
            ServerName = serverName;
            GroupName = groupName;
            ConnectionId = connectionId;
        }
    }
}
