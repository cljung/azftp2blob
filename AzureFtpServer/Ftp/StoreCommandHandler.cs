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

        protected override string OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
                return GetMessage(501, string.Format("{0} needs a parameter", Command));

            string sFile = GetPath(sMessage);

            if (!FileNameHelpers.IsValid(sFile) || sFile.EndsWith(@"/"))
            {
                return GetMessage(553, string.Format("\"{0}\" is not a valid file name", sMessage));
            }

            if (ConnectionObject.FileSystemObject.FileExists(sFile))
            {
                // 2015-11-24 cljung : RFC959 says STOR commands overwrite files, so delete if exists
                if (!StorageProviderConfiguration.FtpOverwriteFileOnSTOR)
                {
                    return GetMessage(553, string.Format("File \"{0}\" already exists.", sMessage));
                }
                Trace.TraceInformation(string.Format("STOR {0} - Deleting existing file", sFile));
                if (!ConnectionObject.FileSystemObject.DeleteFile(sFile))
                {
                    return GetMessage(550, string.Format("Delete file \"{0}\" failed.", sFile));
                }
            }

            var socketData = new FtpDataSocket(ConnectionObject);

            if (!socketData.Loaded)
            {
                return GetMessage(425, "Unable to establish the data connection");
            }

            Trace.TraceInformation(string.Format("STOR {0} - BEGIN", sFile));

            IFile file = ConnectionObject.FileSystemObject.OpenFile(sFile, true);

            if (file == null)
            {
                socketData.Close();// close data socket
                return GetMessage(550, "Couldn't open file");
            }

            SocketHelpers.Send(ConnectionObject.Socket, GetMessage(150, "Opening connection for data transfer."), ConnectionObject.Encoding);

            string md5Value = string.Empty;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // TYPE I, default 
            if (ConnectionObject.DataType == DataType.Image)
            {
                // md5 hash function
                MD5 md5Hash = MD5.Create();

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
                        FtpServer.LogWrite(this, sMessage, 451, sw.ElapsedMilliseconds);
                        return GetMessage(451, "Write data to Azure error!");
                    }
                    md5Hash.TransformBlock(abData, 0, nReceived, null, 0);
                    nReceived = socketData.Receive(abData);
                }
                md5Hash.TransformFinalBlock(new byte[1], 0, 0);
                md5Value = BytesToStr(md5Hash.Hash);                
            }
            // TYPE A
            // won't compute md5, because read characters from client stream
            else if (ConnectionObject.DataType == DataType.Ascii)
            {
                int readSize = SocketHelpers.CopyStreamAscii(socketData.Socket.GetStream(), file.BlobStream, m_nBufferSize);
                FtpServerMessageHandler.SendMessage(ConnectionObject.Id, string.Format("Use ascii type success, read {0} chars!", readSize));
            }
            else { // mustn't reach
                file.Close();
                socketData.Close();
                FtpServer.LogWrite(this, sMessage, 451, sw.ElapsedMilliseconds);
                return GetMessage(451, "Error in transfer data: invalid data type.");
            }

            sw.Stop();
            Trace.TraceInformation( string.Format("STOR {0} - END, Time {1} ms", sFile, sw.ElapsedMilliseconds));

            // upload notification
            ConnectionObject.FileSystemObject.Log4Upload(sFile);

            file.Close();
            socketData.Close();

            // record md5
            ConnectionObject.FileSystemObject.SetFileMd5(sFile, md5Value);

            FtpServer.LogWrite(this, sMessage, 226, sw.ElapsedMilliseconds);
            return GetMessage(226, string.Format("{0} successful. Time {1} ms", Command, sw.ElapsedMilliseconds));
        }

        private static string BytesToStr(byte[] bytes)
        {
            StringBuilder str = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
                str.AppendFormat("{0:X2}", bytes[i]);

            return str.ToString();
        }
    }
}