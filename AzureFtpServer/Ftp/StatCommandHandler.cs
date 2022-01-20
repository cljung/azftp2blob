using AzureFtpServer.General;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.Ftp.General;
using AzureFtpServer.Provider;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// STAT command handler
    /// if the parameter is empty, return status message of this ftp server
    /// otherwise, work as LIST commmand
    /// </summary>
    internal class StatCommandHandler : ListCommandHandlerBase
    {
        public StatCommandHandler(FtpConnectionObject connectionObject)
            : base("STAT", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            
            if (sMessage == "")
            {
                return new FtpResponse(211, "Server status: OK");
            }

            // if no parameter is given, STAT works as LIST
            // but won't use data connection
            IFileInfo[] asFiles = null;
            IFileInfo[] asDirectories = null;

            // Get the file/dir to list
            string targetToList = GetPath(sMessage);

            // checks the file/dir name
            if (!FileNameHelpers.IsValid(targetToList))
            {
                return new FtpResponse(501, $"\"{sMessage}\" is not a valid file/directory name");
            }

            // two vars indicating different list results
            bool targetIsFile = false;
            bool targetIsDir = false;

            // targetToList ends with '/', must be a directory
            if (targetToList.EndsWith(@"/"))
            {
                targetIsFile = false;
                if (ConnectionObject.FileSystemObject.DirectoryExists(targetToList))
                    targetIsDir = true;
            }
            else
            {
                // check whether the target to list is a directory
                if (ConnectionObject.FileSystemObject.DirectoryExists(FileNameHelpers.AppendDirTag(targetToList)))
                    targetIsDir = true;
                // check whether the target to list is a file
                if (ConnectionObject.FileSystemObject.FileExists(targetToList))
                    targetIsFile = true;
            }

            if (targetIsFile)
            {
                asFiles = new IFileInfo[] { ConnectionObject.FileSystemObject.GetFileInfo(targetToList) };
                if (targetIsDir)
                    asDirectories = new IFileInfo[] { ConnectionObject.FileSystemObject.GetDirectoryInfo(targetToList) };
            }
            // list a directory
            else if (targetIsDir)
            {
                targetToList = FileNameHelpers.AppendDirTag(targetToList);
                asFiles = ConnectionObject.FileSystemObject.GetFiles(targetToList);
                asDirectories = ConnectionObject.FileSystemObject.GetDirectories(targetToList, StorageProviderConfiguration.FtpActualDirectoryCreationTime);
            }
            else
            {
                return new FtpResponse(550, $"\"{sMessage}\" not exists");
            }

            // generate the response
            string sFileList = BuildReply(asFiles, asDirectories);

            SocketHelpers.Send(ConnectionObject.Socket, $"213-Begin STAT \"{sMessage}\":\r\n", ConnectionObject.Encoding);

            SocketHelpers.Send(ConnectionObject.Socket, sFileList, ConnectionObject.Encoding);
            
            return new FtpResponse(213, $"{Command} successful.");
        }

        protected override string BuildReply(IFileInfo[] asFiles, IFileInfo[] asDirectories)
        {
            return BuildLongReply(asFiles, asDirectories);
        }
    }
}