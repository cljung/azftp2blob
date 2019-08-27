using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using AzureFtpServer.Ftp;
using AzureFtpServer.General;
using AzureFtpServer.Provider;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// PASV command handler
    /// enter passive mode
    /// </summary>
    internal class PasvCommandHandler : FtpCommandHandler
    {
        private readonly int m_nPort;
        private readonly int maxAcceptWaitTimeSeconds;

        // This command maybe won't work if the ftp server is deployed locally <= firewall
        public PasvCommandHandler(FtpConnectionObject connectionObject)
            : base("PASV", connectionObject)
        {
            // set passive listen port
            m_nPort = int.Parse(ConfigurationManager.AppSettings["FTPPASV"]);
            int maxWaitSeconds;
            if (!int.TryParse(ConfigurationManager.AppSettings["MaxIdleSeconds"], out maxWaitSeconds))
            {
                maxAcceptWaitTimeSeconds = 60;
            }
            else
            {
                maxAcceptWaitTimeSeconds = Math.Max(5, maxWaitSeconds/2);
            }
        }

        /// <summary>
        /// Keeps track to open tcp listener ports, which accept passive requests.
        /// Introduced to overcome the
        /// <c>Only one usage of each socket address is permitted</c>
        /// error whe starting TcpListener on passive port
        /// </summary>
        private static readonly HashSet<int> ListeningPassivePorts = new HashSet<int>();
        protected override FtpResponse OnProcess(string sMessage)
        {
            ConnectionObject.DataConnectionType = DataConnectionType.Passive;

            //return GetMessage(227, string.Format("Entering Passive Mode ({0})", pasvListenAddress));

            // listen at the port by the "FTP" endpoint setting
            int port = int.Parse(ConfigurationManager.AppSettings["FTPPASV"]);
            int maxPort = port + int.Parse(ConfigurationManager.AppSettings["MaxClients"]);

            int selectedPort = port;
            lock (ListeningPassivePorts)
            {
                while (selectedPort < maxPort && !ListeningPassivePorts.Add(selectedPort))
                {
                    selectedPort++;
                }
            }

            if (selectedPort == maxPort)
            {
                FtpServer.LogWrite("unable to select passive ports, looks like too many clients are connected at once");
                return new FtpResponse(550, "Too many concurrent PASV requests");
            }

            string pasvListenAddress = GetPassiveAddressInfo(selectedPort);

            //System.Net.IPAddress ipaddr = SocketHelpers.GetLocalAddress();
            //System.Net.IPEndPoint ipEndPoint = new System.Net.IPEndPoint(ipaddr.Address, port);
            System.Net.IPEndPoint ipEndPoint = new System.Net.IPEndPoint(IPAddress.Any, selectedPort);

            TcpListener listener = SocketHelpers.CreateTcpListener( ipEndPoint );
            if (listener == null)
            {
                return new FtpResponse(550, $"Couldn't start listener on port {m_nPort}");
            }

            try
            {
                Trace.TraceInformation($"Entering Passive Mode on {pasvListenAddress}");
                SocketHelpers.Send(ConnectionObject.Socket, $"227 Entering Passive Mode ({pasvListenAddress})\r\n",
                    ConnectionObject.Encoding);

                listener.Start();

                Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
                int completed = Task.WaitAny(new[] {acceptTask}, TimeSpan.FromSeconds(maxAcceptWaitTimeSeconds));
                if (completed != 0)
                {
                    FtpServer.LogWrite("timeout while waiting on PASV connection");
                    return new FtpResponse(550, "PASV listener timeout");
                }

                ConnectionObject.PassiveSocket = acceptTask.Result;
            }
            finally
            {
                listener.Stop();
                lock (ListeningPassivePorts)
                {
                    ListeningPassivePorts.Remove(selectedPort);
                }
            }

            return new FtpResponse(0,"");
        }

        private string GetPassiveAddressInfo(int port)
        {
            // get routable ipv4 address of load balanced service
            IPAddress ipAddress = SocketHelpers.GetLocalAddress(StorageProviderConfiguration.FtpServerHostPublic);
            if (ipAddress == null)
            {
                throw new Exception("The ftp server do not have a ipv4 address");
            }
            string retIpPort = ipAddress.ToString();
            retIpPort = retIpPort.Replace('.', ',');

            // append the port
            retIpPort += ',';
            retIpPort += (port / 256).ToString();
            retIpPort += ',';
            retIpPort += (port % 256).ToString();

            return retIpPort;
        }
    }
}