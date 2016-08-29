using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AzureFtpServer.Extensions
{
    internal static class SocketExt
    {
        public static string GetRemoteAddrSafelly(this TcpClient socket)
        {
            try
            {
                return socket?.Client?.RemoteEndPoint?.ToString() ?? "NULL";
            }
            catch (Exception e)
            {
                return $"UNKNOWN [{e.Message}]";
            }
        }
    }
}
