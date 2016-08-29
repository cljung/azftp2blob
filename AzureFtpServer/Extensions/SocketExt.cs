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

        public static void CloseSafelly(this TcpClient socket)
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                if (socket.GetStream() != null)
                {
                    try
                    {
                        socket.GetStream().Flush();
                    }
                    catch (SocketException)
                    {
                    }

                    try
                    {
                        socket.GetStream().Close();
                    }
                    catch (SocketException)
                    {
                    }
                }
            }
            catch (SocketException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                socket.Close();
            }
            catch (SocketException)
            {
            }
        }
    }
}
