﻿using Microsoft.AspNetCore.SignalR.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Tests
{
    public static class HubProtocolHelpers
    {
        private static readonly IHubProtocol JsonHubProtocol = new JsonHubProtocol();

        private static readonly IHubProtocol MessagePackHubProtocol = new MessagePackHubProtocol();

        public static readonly List<string> AllProtocolNames = new List<string>
        {
            JsonHubProtocol.Name,
            MessagePackHubProtocol.Name
        };

        public static readonly IList<IHubProtocol> AllProtocols = new List<IHubProtocol>()
        {
            JsonHubProtocol,
            MessagePackHubProtocol
        };

        public static IHubProtocol GetHubProtocol(string name)
        {
            var protocol = AllProtocols.SingleOrDefault(p => p.Name == name);
            if (protocol == null)
            {
                throw new InvalidOperationException($"Could not find protocol with name '{name}'.");
            }

            return protocol;
        }
    }
}
