using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Internal
{
    public readonly struct RabbitMQInvocation
    {
        public IReadOnlyList<string> ExcludedConnectionIds { get; }
        public SerializedHubMessage Message { get; }
    
        public RabbitMQInvocation(SerializedHubMessage message, IReadOnlyList<string> excludedConnectionIds)
        {
            this.Message = message;
            this.ExcludedConnectionIds = excludedConnectionIds;
        }
    }
}
