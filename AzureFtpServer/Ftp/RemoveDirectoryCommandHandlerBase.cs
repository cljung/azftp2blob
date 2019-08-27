using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// base class for RMD & XRMD command handlers
    /// </summary>
    internal class RemoveDirectoryCommandHandlerBase : FtpCommandHandler
    {
        protected RemoveDirectoryCommandHandlerBase(string sCommand, FtpConnectionObject connectionObject)
            : base(sCommand, connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
            {
                return new FtpResponse(501, $"{Command} needs a parameter");
            }

            string dirToRemove = GetPath(FileNameHelpers.AppendDirTag(sMessage));

            // check whether directory exists
            if (!ConnectionObject.FileSystemObject.DirectoryExists(dirToRemove))
            {
                return new FtpResponse(550, $"Directory \"{dirToRemove}\" does not exist");
            }

            // can not delete root directory
            if (dirToRemove == "/")
            {
                return new FtpResponse(553, "Can not remove root directory");
            }

            // delete directory
            if (ConnectionObject.FileSystemObject.DeleteDirectory(dirToRemove))
            {
                return new FtpResponse(250, $"{Command} successful.");
            }
            return new FtpResponse(550, $"Couldn't remove directory ({dirToRemove}).");
        }
    }
}