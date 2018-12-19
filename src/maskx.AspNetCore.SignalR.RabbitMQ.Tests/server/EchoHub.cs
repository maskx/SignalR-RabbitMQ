using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Tests
{
    public class EchoHub : Hub
    {
        public string Echo(string message)
        {
            return message;
        }

        public Task EchoGroup(string groupName, string message)
        {
            return Clients.Group(groupName).SendAsync("Echo", message);
        }

        public Task EchoUser(string userName, string message)
        {
            return Clients.User(userName).SendAsync("Echo", message);
        }

        public Task AddSelfToGroup(string groupName)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
