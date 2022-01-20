using System.Text;
using AzureFtpServer.General;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.Ftp.General;
using AzureFtpServer.Provider;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// MLSD command handler
    /// only list content under directories
    /// </summary>
    class MlsdCommandHandler : MlsxCommandHandlerBase
    {
        public MlsdCommandHandler(FtpConnectionObject connectionObject)
            : base("MLSD", connectionObject)
        { 
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();

            // Get the dir to list
            string targetToList = GetPath(sMessage);

            // checks the dir name
            if (!FileNameHelpers.IsValid(targetToList))
            {
                return new FtpResponse(501, $"\"{sMessage}\" is not a valid directory name");
            }

            // specify the directory tag
            targetToList = FileNameHelpers.AppendDirTag(targetToList);

            bool targetIsDir = ConnectionObject.FileSystemObject.DirectoryExists(targetToList);

            if (!targetIsDir)
            {
                return new FtpResponse(550, $"Directory \"{targetToList}\" not exists");
            }

            #region Generate response

            StringBuilder response = new StringBuilder();

            IFileInfo[] files = ConnectionObject.FileSystemObject.GetFiles(targetToList);
            IFileInfo[] directories = ConnectionObject.FileSystemObject.GetDirectories(targetToList, StorageProviderConfiguration.FtpActualDirectoryCreationTime);

            if (files != null)
            {
                foreach (var file in files)
                { 
                    response.Append(GenerateEntry(file));
                    response.Append("\r\n");
                }
            }

            if (directories != null)
            {
                foreach (var dir in directories)
                {
                    response.Append(GenerateEntry(dir));

                    response.Append("\r\n");
                }
            }

            #endregion

            #region Write response

            var socketData = new FtpDataSocket(ConnectionObject);

            if (!socketData.Loaded)
            {
                return new FtpResponse(425, "Unable to establish the data connection");
            }

            SocketHelpers.Send(ConnectionObject.Socket, "150 Opening data connection for MLSD\r\n", ConnectionObject.Encoding);

            // ToDo, send response according to ConnectionObject.DataType, i.e., Ascii or Binary
            socketData.Send(response.ToString(), Encoding.UTF8);
            socketData.Close();

            #endregion

            return new FtpResponse(226, "MLSD successful");
        }
    }
}
