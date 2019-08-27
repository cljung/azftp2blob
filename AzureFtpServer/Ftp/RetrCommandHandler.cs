using System.Text;
using System.Diagnostics;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// RETR command handler
    /// retreive file from ftp server
    /// </summary>
    internal class RetrCommandHandler : FtpCommandHandler
    {
        private const int m_nBufferSize = 1048576;

        public RetrCommandHandler(FtpConnectionObject connectionObject)
            : base("RETR", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
            {
                return new FtpResponse(501, $"{Command} needs a parameter");
            }

            string sFilePath = GetPath(sMessage);
            Trace.TraceInformation($"RETR {sFilePath} - BEGIN");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (!ConnectionObject.FileSystemObject.FileExists(sFilePath))
            {
                return new FtpResponse(550, $"File \"{sMessage}\" doesn't exist");
            }

            var socketData = new FtpDataSocket(ConnectionObject);
            IFile file = null;
            try
            {
                if (!socketData.Loaded)
                {
                    return new FtpResponse(425, "Unable to establish the data connection");
                }

                SocketHelpers.Send(ConnectionObject.Socket, "150 Starting data transfer, please wait...\r\n",
                    ConnectionObject.Encoding);

                file = ConnectionObject.FileSystemObject.OpenFile(sFilePath, false);
                if (file == null)
                {
                    return new FtpResponse(550, "Couldn't open file");
                }

                // TYPE I, default
                if (ConnectionObject.DataType == DataType.Image)
                {
                    var abBuffer = new byte[m_nBufferSize];

                    int nRead = file.Read(abBuffer, m_nBufferSize);

                    while (nRead > 0 && socketData.Send(abBuffer, nRead))
                    {
                        nRead = file.Read(abBuffer, m_nBufferSize);
                    }
                }
                // TYPE A
                else if (ConnectionObject.DataType == DataType.Ascii)
                {
                    int writeSize = SocketHelpers.CopyStreamAscii(file.BlobStream, socketData.Socket.GetStream(),
                        m_nBufferSize);
                    FtpServerMessageHandler.SendMessage(ConnectionObject.Id, 
                        $"Use ascii type success, write {writeSize} chars!");
                }
                else // mustn't reach
                {
                    file.Close();
                    socketData.Close();
                    return new FtpResponse(451, "Error in transfer data: invalid data type.");
                }

                sw.Stop();
                Trace.TraceInformation($"RETR {sFilePath} - END, Time {sw.ElapsedMilliseconds} ms");

                return new FtpResponse(226, $"File download succeeded. Time {sw.ElapsedMilliseconds} ms");
            }
            finally
            {
                file?.Close();
                socketData.Close();
            }
        }
    }
}