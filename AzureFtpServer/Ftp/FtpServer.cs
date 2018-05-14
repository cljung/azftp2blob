using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Configuration;
using System.Net;
using System.Linq;
using AzureFtpServer.Extensions;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.General;
using AzureFtpServer.FtpCommands;
using AzureFtpServer.Security;

namespace AzureFtpServer.Ftp
{
    /// <summary>
    /// Listens for incoming connections and accepts them.
    /// Incomming socket connections are then passed to the socket handling class (FtpSocketHandler).
    /// </summary>
    public class FtpServer
    {
        #region Member Variables

        private readonly List<FtpSocketHandler> m_apConnections;
        private readonly IFileSystemClassFactory m_fileSystemClassFactory;
        private readonly InvalidAttemptCounter m_invalidLogonCounter;

        private int m_nId;
        private TcpListener m_socketListen;
        private Thread m_theThread;
        private bool m_started = false;
        private Encoding m_encoding;
        private int m_maxClients;
        private static bool m_logEnabled = false;
        private static string m_logPath = "";
        private static string ComputerName = "";
        private static string m_ftpIpAddr = "";

        #endregion

        #region Events

        #region Delegates

        public delegate void ConnectionHandler(int nId);

        #endregion

        public event ConnectionHandler ConnectionClosed;
        public event ConnectionHandler NewConnection;

        #endregion

        #region Construction

        public FtpServer(IFileSystemClassFactory fileSystemClassFactory)
        {
            m_apConnections = new List<FtpSocketHandler>();
            m_fileSystemClassFactory = fileSystemClassFactory;

            InvalidLogonCheckOptions logonCheckOpts = new InvalidLogonCheckOptions();
            m_invalidLogonCounter = new InvalidAttemptCounter(logonCheckOpts);
        }

        ~FtpServer()
        {
            m_socketListen?.Stop();
        }

        public bool Started => m_started;

        #endregion

        #region Methods

        public void Start()
        {
            // initialise the encoding of the control channel
            InitialiseConnectionEncoding();
            
            // initialise the max number of clients
            InitialiseMaxClients();

            InitialiseLogging();

            m_theThread = new Thread(ThreadRun);
            m_theThread.Start();
            m_started= true;
        }

        public void Stop()
        {
            for (int nConnection = 0; nConnection < m_apConnections.Count; nConnection++)
            {
                var handler = m_apConnections[nConnection] as FtpSocketHandler;
                handler.Stop();
            }

            m_socketListen.Stop();
            m_theThread.Join();
            m_started = false;
        }

        /// <summary>
        /// The main thread of the ftp server
        /// Listen and acception clients, create handler threads for each client
        /// </summary>
        private void ThreadRun()
        {
            FtpServerMessageHandler.Message += TraceMessage;

            // listen at the port by the "FTP" endpoint setting
            int port = int.Parse(ConfigurationManager.AppSettings["FTP"]);
            System.Net.IPAddress ipaddr = SocketHelpers.GetLocalAddress();
            //System.Net.IPEndPoint ipEndPoint = new System.Net.IPEndPoint(ipaddr.Address, port);
            System.Net.IPEndPoint ipEndPoint = new System.Net.IPEndPoint(IPAddress.Any, port);
            FtpServer.m_ftpIpAddr = ipaddr.ToString();
            m_socketListen = SocketHelpers.CreateTcpListener(ipEndPoint);

            if (m_socketListen == null)
            {
                FtpServerMessageHandler.SendMessage(0, "Error in starting FTP server");
                return;
            }

            string msg = $"FTP Server started.Listening to: {ipEndPoint}";
            FtpServer.LogWrite(msg);
            Trace.TraceInformation(msg);

            m_socketListen.Start();
            bool fContinue = true;

            while (fContinue)
            {
                TcpClient socket = null;

                try
                {
                    socket = m_socketListen.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    fContinue = false;
                }
                finally
                {
                    if (socket == null)
                    {
                        fContinue = false;
                    }
                    else if (m_apConnections.Count >= m_maxClients)
                    {
                        Trace.WriteLine("Too many clients, won't handle this connection", "Warning");
                        SendRejectMessage(socket);
                        socket.CloseSafelly();
                    }
                    else
                    {
                        socket.NoDelay = false;

                        m_nId++;

                        FtpServerMessageHandler.SendMessage(m_nId, "New connection");

                        SendAcceptMessage(socket);
                        // 2015-11-25 cljung : under stress testing, this happens. Don't know why yet, but let's keep it from crashing
                        try
                        {
                            InitialiseSocketHandler(socket);
                        }
                        catch (System.ObjectDisposedException ode)
                        {
                            FtpServer.LogWrite($"ObjectDisposedException initializing client socket:\r\n{ode}");
                            Trace.TraceError($"ObjectDisposedException initializing client socket:\r\n{ode}");
                            m_nId--;
                            socket.CloseSafelly();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Init the encoding of the control channel by the Role setting "ConnectionEncoding"
        /// If the value is "ASCII", encoding = Encoding.ASCII
        /// Otherwise, m_encoding = Encoding.UTF8
        /// </summary>
        private void InitialiseConnectionEncoding()
        {
            string encoding = ConfigurationManager.AppSettings["ConnectionEncoding"];
            switch (encoding)
            {
                case "ASCII":
                    m_encoding = Encoding.ASCII;
                    Trace.WriteLine("Set ftp connection encoding: ASCII", "Information");
                    break;
                case "UTF8":
                default:
                    m_encoding = Encoding.UTF8;
                    Trace.WriteLine("Set ftp connection encoding: UTF8", "Information");
                    break;
            }
        }

        /// <summary>
        /// Init the member variable m_maxClient by the Role setting "MaxClients"
        /// If any exception or error happens, use default value 5
        /// </summary>
        private void InitialiseMaxClients()
        {
            string maxClients = ConfigurationManager.AppSettings["MaxClients"];

            int iMaxClients = 5;
            
            try
            {
                iMaxClients = Convert.ToInt32(maxClients);
            }
            catch (Exception)
            { 
                // if the "MaxClients" setting is invalid to convert into integer, use default value
                Trace.WriteLine($"Invalid MaxClients setting: {maxClients}", "Warnning");
            }

            if (iMaxClients <= 0)
            { 
                // negtive or 0 is also invalid, use default value
                iMaxClients = 5;
            }

            m_maxClients = iMaxClients;
        }
        private void InitialiseLogging()
        {
            string buf = ConfigurationManager.AppSettings["LoggingEnabled"];
            FtpServer.m_logEnabled = buf.ToLowerInvariant() == "true";
            if (!FtpServer.m_logEnabled)
                return;
            FtpServer.m_logPath = ConfigurationManager.AppSettings["LogPath"];
            FtpServer.ComputerName = Environment.MachineName;       

            try
            {
                string dirPath = Path.IsPathRooted(m_logPath) ? m_logPath : Path.GetFullPath(m_logPath);
                if (!Directory.Exists(dirPath))
                {
                    Trace.TraceInformation($"creating log directory {dirPath}");
                    Directory.CreateDirectory(dirPath);
                }

                string filename = Path.Combine(FtpServer.m_logPath, "testfile.txt");
                File.WriteAllText(filename, "test content to see if we have write access");
                File.Delete(filename);
            }
            catch (Exception ex)
            {
                // if the "MaxClients" setting is invalid to convert into integer, use default value
                Trace.TraceError($"Error writing to log directory {FtpServer.m_logPath} - {ex.Message}");
                FtpServer.m_logEnabled = false;
                FtpServer.m_logPath = "";
            }
        }

        private void SendAcceptMessage(TcpClient socket)
        {
            SocketHelpers.Send(socket, m_encoding.GetBytes(
                $"220 Azure Blob Storage FTP Proxy Server - {Environment.MachineName}\r\n"));
        }

        private void SendRejectMessage(TcpClient socket)
        {
            lock (m_apConnections)
            {
                FtpServer.LogWrite(
                    $"{socket.Client.RemoteEndPoint} 421 Too many users now. Count {m_apConnections.Count}, Max {m_maxClients}"
                    );

                string clientsDump = string.Join("\r\n", m_apConnections.Select(c => $"\t{c.RemoteEndPoint}"));
                FtpServer.LogWrite($"connected clients:\r\n{clientsDump}");

                SocketHelpers.Send(socket, m_encoding.GetBytes("421 Too many users now\r\n"));
            }
        }

        private void InitialiseSocketHandler(TcpClient socket)
        {
            lock (m_apConnections)
            {
                var handler = new FtpSocketHandler(m_fileSystemClassFactory, m_nId);
                handler.Closed += handler_Closed;

                m_apConnections.Add(handler);

                FtpServer.LogWrite(
                    $"Client accepted: {socket.Client.RemoteEndPoint} current count={m_apConnections.Count}");
                Trace.WriteLine(
                    $"Handler created for client {handler.RemoteEndPoint}. Current Count {m_apConnections.Count}",
                    "Information");

                NewConnection?.Invoke(m_nId);

                // get encoding for the socket connection
                // Start must be moved at the end
                // otherwise we have a race condition: client can cause connection close
                // before we log new connection, which will result in ObjectDisposedException
                // (when an attempt to access RemoteEndPoint is made)
                handler.Start(socket, m_encoding, m_invalidLogonCounter);
            }
        }

        #endregion

        #region Event Handlers

        private void handler_Closed(FtpSocketHandler handler)
        {
            lock (m_apConnections)
            {
                m_apConnections.Remove(handler);

                LogWrite(
                    $"Client closed: {handler.RemoteEndPoint} current count={m_apConnections.Count}");
                Trace.WriteLine(
                    $"Handler closed {handler.RemoteEndPoint}. Current Count {m_apConnections.Count}",
                    "Information");

                ConnectionClosed?.Invoke(handler.Id);
            }
        }

        public void TraceMessage(int nId, string sMessage)
        {
            Trace.WriteLine($"{nId}: {sMessage}", "FtpServerMessage");
        }

        private static string FileName => $"ftplog_{DateTime.UtcNow:yyyyMMdd}_{FtpServer.ComputerName}.log";
        private static readonly object LogFileLock = new object();

        public static void LogWrite(string comment)
        {
            if (!m_logEnabled)
            {
                return;
            }

            try
            {
                DateTime utcNow = DateTime.UtcNow;
                string filename = Path.Combine(FtpServer.m_logPath, FileName);
                string logdata = $"{utcNow:yyyy-MM-dd HH:mm:ss} {comment}\r\n";
                lock (LogFileLock)
                {
                    File.AppendAllText(filename, logdata);
                }
            }
            catch 
            {
                // can't fail
            }
        }
        public static void LogWrite( FtpCommandHandler ch, string sMessage, int retCode, long elapsedMs )
        {
            if (!m_logEnabled)
                return;

            try
            {
                DateTime utcNow = DateTime.UtcNow;
                string filename = Path.Combine(FtpServer.m_logPath, FileName);
                // 2015-11-20 23:13:57 213.67.145.199 CLJUNGFTP01\hhh 10.76.190.155 21 RETR 151118+HH-RIG+3-0.avi 226 0 0 5937c9cb-07d9-4fa8-a04d-3bff7fd024e9 /herr/elitserien/1-grundserien/151118+HH-RIG+3-0.avi

                string logdata = string.Format("{0} {1} {2} {3} {4} {5} {6} {7}\r\n"
                                            , utcNow.ToString("yyyy-MM-dd HH:mm:ss")
                                            , ch.ConnectionObject.Socket.Client.RemoteEndPoint.ToString()
                                            , ch.ConnectionObject.User
                                            , FtpServer.m_ftpIpAddr
                                            , ch.Command
                                            , retCode
                                            , elapsedMs
                                            , sMessage
                                            );
                lock (LogFileLock)
                {
                    File.AppendAllText(filename, logdata);
                }
            }
            catch 
            {
                // can't fail
            }
        }

        #endregion
    }
}