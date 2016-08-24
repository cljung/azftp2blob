using System.Diagnostics;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.General;
using AzureFtpServer.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// APPE command handler
    /// append content to a file(either existed or non-existed)
    /// Only for blockblob
    /// </summary>
    internal class AppendCommandHandler : FtpCommandHandler
    {
        private const int m_nBufferSize = 65536;

        public AppendCommandHandler(FtpConnectionObject connectionObject)
            : base("APPE", connectionObject)
        {
        }

        protected override string OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
                return GetMessage(501, string.Format("{0} needs a parameter", Command));

            string sFile = GetPath(sMessage);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (!FileNameHelpers.IsValid(sFile))
            {
                FtpServer.LogWrite(this, sMessage, 553, sw.ElapsedMilliseconds);
                return GetMessage(553, string.Format("\"{0}\" is not a valid file name", sMessage));
            }

            var socketData = new FtpDataSocket(ConnectionObject);

            if (!socketData.Loaded)
            {
                return GetMessage(425, "Unable to establish the data connection");
            }

            SocketHelpers.Send(ConnectionObject.Socket, GetMessage(150, "Opening connection for data transfer."), ConnectionObject.Encoding);

            if (!ConnectionObject.FileSystemObject.AppendFile(sFile, socketData.Socket.GetStream()))
            {
                FtpServer.LogWrite(this, sMessage, 553, sw.ElapsedMilliseconds);
                return GetMessage(553, $"{Command} error");
            }

            sw.Stop();
            FtpServer.LogWrite(this, sMessage, 250, sw.ElapsedMilliseconds);
            return GetMessage(250, $"{Command} successful");
        }
    }
}