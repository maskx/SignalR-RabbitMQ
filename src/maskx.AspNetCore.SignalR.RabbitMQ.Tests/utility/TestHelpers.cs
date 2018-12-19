using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace maskx.AspNetCore.SignalR.RabbitMQ.Tests
{
    public static class TestHelpers
    {
        public static bool IsWebSocketsSupported()
        {
#if NETCOREAPP3_0
            // .NET Core 2.1 and greater has sockets
            return true;
#else
            // Non-Windows platforms have sockets
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return true;
            }

            // Windows 8 and greater has sockets
            return Environment.OSVersion.Version >= new Version(6, 2);
#endif
        }
    }
}
