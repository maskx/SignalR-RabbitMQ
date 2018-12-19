using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Internal
{
    internal enum RabbitMQChannel
    {
        All,
        Group,
        Groups,
        Connection,
        Connections,
        User,
        Users,
        GroupCommand,
        Ack
    }
}
