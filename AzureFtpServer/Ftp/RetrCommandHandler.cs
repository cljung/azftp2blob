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

        protected override string OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
                return GetMessage(501, string.Format("{0} needs a parameter", Command));
            
            string sFilePath = GetPath(sMessage);
            Trace.TraceInformation(string.Format("RETR {0} - BEGIN", sFilePath));
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (!ConnectionObject.FileSystemObject.FileExists(sFilePath))
            {
                FtpServer.LogWrite(this, sMessage, 550, sw.ElapsedMilliseconds);
                return GetMessage(550, string.Format("File \"{0}\" doesn't exist", sMessage));
            }

            var socketData = new FtpDataSocket(ConnectionObject);
            
            if (!socketData.Loaded)
            {
                FtpServer.LogWrite(this, sMessage, 425, sw.ElapsedMilliseconds);
                return GetMessage(425, "Unable to establish the data connection");
            }

            SocketHelpers.Send(ConnectionObject.Socket, "150 Starting data transfer, please wait...\r\n", ConnectionObject.Encoding);
            
            IFile file = ConnectionObject.FileSystemObject.OpenFile(sFilePath, false);

            if (file == null)
            {
                return GetMessage(550, "Couldn't open file");
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
                int writeSize = SocketHelpers.CopyStreamAscii(file.BlobStream, socketData.Socket.GetStream(), m_nBufferSize);
                FtpServerMessageHandler.SendMessage(ConnectionObject.Id, string.Format("Use ascii type success, write {0} chars!", writeSize));
            }
            else // mustn't reach
            {
                file.Close();
                socketData.Close();
                FtpServer.LogWrite(this, sMessage, 451, sw.ElapsedMilliseconds);
                return GetMessage(451, "Error in transfer data: invalid data type.");
            }

            file.Close();
            socketData.Close();

            sw.Stop();
            Trace.TraceInformation(string.Format("RETR {0} - END, Time {1} ms", sFilePath, sw.ElapsedMilliseconds));

            FtpServer.LogWrite(this, sMessage, 226, sw.ElapsedMilliseconds);
            return GetMessage(226, string.Format("File download succeeded. Time {0} ms", sw.ElapsedMilliseconds));
        }
    }
}