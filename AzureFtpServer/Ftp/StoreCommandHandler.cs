using System.Text;
using System.Diagnostics;
using System.Configuration;
using System.Security.Cryptography;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.General;
using AzureFtpServer.Ftp.General;
using AzureFtpServer.Provider;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// STOR command handler
    /// upload file to the ftp server
    /// </summary>
    internal class StoreCommandHandler : FtpCommandHandler
    {
        private const int m_nBufferSize = 1048576;

        public StoreCommandHandler(FtpConnectionObject connectionObject)
            : base("STOR", connectionObject)
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

            if (!FileNameHelpers.IsValid(sFile) || sFile.EndsWith(@"/"))
            {
                return new FtpResponse(553, $"\"{sMessage}\" is not a valid file name");
            }

            if (ConnectionObject.FileSystemObject.FileExists(sFile))
            {
                // 2015-11-24 cljung : RFC959 says STOR commands overwrite files, so delete if exists
                if (!StorageProviderConfiguration.FtpOverwriteFileOnSTOR)
                {
                    return new FtpResponse(553, $"File \"{sMessage}\" already exists.");
                }
                Trace.TraceInformation($"STOR {sFile} - Deleting existing file");
                if (!ConnectionObject.FileSystemObject.DeleteFile(sFile))
                {
                    return new FtpResponse(550, $"Delete file \"{sFile}\" failed.");
                }
            }

            var socketData = new FtpDataSocket(ConnectionObject);
            IFile file = null;

            try
            {
                if (!socketData.Loaded)
                {
                    return new FtpResponse(425, "Unable to establish the data connection");
                }

                Trace.TraceInformation($"STOR {sFile} - BEGIN");

                file = ConnectionObject.FileSystemObject.OpenFile(sFile, true);
                if (file == null)
                {
                    socketData.Close(); // close data socket
                    return new FtpResponse(550, "Couldn't open file");
                }

                SocketHelpers.Send(ConnectionObject.Socket, new FtpResponse(150, "Opening connection for data transfer."),
                    ConnectionObject.Encoding);

                Stopwatch sw = new Stopwatch();
                sw.Start();
                // TYPE I, default 
                if (ConnectionObject.DataType == DataType.Image)
                {
                    var abData = new byte[m_nBufferSize];

                    int nReceived = socketData.Receive(abData);

                    while (nReceived > 0)
                    {
                        int writeSize = file.Write(abData, nReceived);
                        // maybe error
                        if (writeSize != nReceived)
                        {
                            file.Close();
                            socketData.Close();
                            return new FtpResponse(451, "Write data to Azure error!");
                        }
                        nReceived = socketData.Receive(abData);
                    }
                }
                // TYPE A
                // won't compute md5, because read characters from client stream
                else if (ConnectionObject.DataType == DataType.Ascii)
                {
                    int readSize = SocketHelpers.CopyStreamAscii(socketData.Socket.GetStream(), file.BlobStream,
                        m_nBufferSize);
                    FtpServerMessageHandler.SendMessage(ConnectionObject.Id,
                        $"Use ascii type success, read {readSize} chars!");
                }
                else
                {
                    // mustn't reach
                    file.Close();
                    socketData.Close();
                    return new FtpResponse(451, "Error in transfer data: invalid data type.");
                }

                sw.Stop();
                Trace.TraceInformation($"STOR {sFile} - END, Time {sw.ElapsedMilliseconds} ms");

                // upload notification
                ConnectionObject.FileSystemObject.Log4Upload(sFile);
                return new FtpResponse(226, $"{Command} successful. Time {sw.ElapsedMilliseconds} ms");
            }
            finally
            {
                file?.Close();
                socketData.Close();
            }
        }
    }
}