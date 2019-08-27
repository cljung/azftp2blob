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

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
            {
                return new FtpResponse(501, $"{Command} needs a parameter");
            }

            string sFile = GetPath(sMessage);

            if (!FileNameHelpers.IsValid(sFile))
            {
                return new FtpResponse(553, $"\"{sMessage}\" is not a valid file name");
            }

            var socketData = new FtpDataSocket(ConnectionObject);
            if (!socketData.Loaded)
            {
                return new FtpResponse(425, "Unable to establish the data connection");
            }

            SocketHelpers.Send(ConnectionObject.Socket, new FtpResponse(150, "Opening connection for data transfer."), ConnectionObject.Encoding);

            if (!ConnectionObject.FileSystemObject.AppendFile(sFile, socketData.Socket.GetStream()))
            {
                return new FtpResponse(553, $"{Command} error");
            }

            return new FtpResponse(250, $"{Command} successful");
        }
    }
}