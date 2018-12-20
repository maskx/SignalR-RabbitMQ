using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace maskx.AspNetCore.SignalR.RabbitMQ
{
    public class RabbitMQOptions
    {
        public string ExchangeName { get; set; }
        public ConnectionFactory ConnectionFactory { get; set; }
    }
}
