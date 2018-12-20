using Microsoft.Extensions.Logging.Testing;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Tests
{
    // WriteContext, but with a timestamp...
    internal class LogRecord
    {
        public DateTime Timestamp { get; }
        public WriteContext Write { get; }

        public LogRecord(DateTime timestamp, WriteContext write)
        {
            Timestamp = timestamp;
            Write = write;
        }
    }
}
