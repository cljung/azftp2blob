using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// base class for MKD & XMKD command handlers
    /// </summary>
    internal class MakeDirectoryCommandHandlerBase : FtpCommandHandler
    {
        protected MakeDirectoryCommandHandlerBase(string sCommand, FtpConnectionObject connectionObject)
            : base(sCommand, connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
            {
                return new FtpResponse(500, $"{Command} needs a paramter");
            }

            string dirToMake = GetPath(FileNameHelpers.AppendDirTag(sMessage));

            // check directory name
            if (!FileNameHelpers.IsValid(dirToMake))
            {
                return new FtpResponse(553, $"\"{sMessage}\": Invalid directory name");
            }
            // check whether directory already exists
            if (ConnectionObject.FileSystemObject.DirectoryExists(dirToMake))
            {
                return new FtpResponse(553, $"Directory \"{sMessage}\" already exists");
            }
            // create directory
            if (!ConnectionObject.FileSystemObject.CreateDirectory(dirToMake))
            {
                return new FtpResponse(550, $"Couldn't create directory. ({sMessage})");
            }

            return new FtpResponse(257, $"{Command} successful \"{dirToMake}\".");
        }
    }
}