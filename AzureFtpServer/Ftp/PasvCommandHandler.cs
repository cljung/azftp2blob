using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
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
        private int m_nPort;

        // This command maybe won't work if the ftp server is deployed locally <= firewall
        public PasvCommandHandler(FtpConnectionObject connectionObject)
            : base("PASV", connectionObject)
        {
            // set passive listen port
            m_nPort = int.Parse(ConfigurationManager.AppSettings["FTPPASV"]);
        }

        /// <summary>
        /// Keeps track to open tcp listener ports, which accept passive requests.
        /// Introduced to overcome the
        /// <c>Only one usage of each socket address is permitted</c>
        /// error whe starting TcpListener on passive port
        /// </summary>
        private static readonly HashSet<int> ListeningPassivePorts = new HashSet<int>();
        protected override string OnProcess(string sMessage)
        {
            ConnectionObject.DataConnectionType = DataConnectionType.Passive;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            string pasvListenAddress = GetPassiveAddressInfo();

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
                return GetMessage(550, "Too many concurrent PASV requests");
            }

            //System.Net.IPAddress ipaddr = SocketHelpers.GetLocalAddress();
            //System.Net.IPEndPoint ipEndPoint = new System.Net.IPEndPoint(ipaddr.Address, port);
            System.Net.IPEndPoint ipEndPoint = new System.Net.IPEndPoint(IPAddress.Any, selectedPort);

            TcpListener listener = SocketHelpers.CreateTcpListener( ipEndPoint );
            if (listener == null)
            {
                FtpServer.LogWrite(this, sMessage, 550, 0);
                return GetMessage(550, $"Couldn't start listener on port {m_nPort}");
            }

            try
            {
                Trace.TraceInformation($"Entering Passive Mode on {pasvListenAddress}");
                SocketHelpers.Send(ConnectionObject.Socket, $"227 Entering Passive Mode ({pasvListenAddress})\r\n",
                    ConnectionObject.Encoding);

                listener.Start();

                ConnectionObject.PassiveSocket = listener.AcceptTcpClient();
            }
            finally
            {
                listener.Stop();
                sw.Stop();
                lock (ListeningPassivePorts)
                {
                    ListeningPassivePorts.Remove(selectedPort);
                }
            }
            FtpServer.LogWrite(this, sMessage, 0, sw.ElapsedMilliseconds);

            return "";
        }

        private string GetPassiveAddressInfo()
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
            retIpPort += (m_nPort / 256).ToString();
            retIpPort += ',';
            retIpPort += (m_nPort % 256).ToString();

            return retIpPort;
        }
    }
}