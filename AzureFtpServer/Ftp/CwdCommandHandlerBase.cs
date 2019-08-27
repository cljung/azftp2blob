using System.IO;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// base class for CWD & XCWD command handlers
    /// </summary>
    internal class CwdCommandHandlerBase : FtpCommandHandler
    {
        public CwdCommandHandlerBase(string sCommand, FtpConnectionObject connectionObject)
            : base(sCommand, connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage.Length == 0)
            {
                return new FtpResponse(501, $"Syntax error. {Command} needs a parameter");
            }

            // append the final '/' char
            string sMessageFull = FileNameHelpers.AppendDirTag(sMessage);

            #region change to the parent dir
            if (sMessageFull == @"../")
            {
                // get the parent directory
                string parentDir = GetParentDir();
                if (parentDir == null)
                    return new FtpResponse(550, "Root directory, cannot change to the parent directory");

                ConnectionObject.CurrentDirectory = parentDir;
                return new FtpResponse(200, $"{Command} Successful ({parentDir})");
            }
            #endregion
            
            if (!FileNameHelpers.IsValid(sMessageFull))
            {
                return new FtpResponse(550, $"\"{sMessage}\" is not a valid directory string.");
            }

            // get the new directory path
            string newDirectory = GetPath(sMessageFull);

            // checks whether the new directory exists
            if (!ConnectionObject.FileSystemObject.DirectoryExists(newDirectory))
            {
                return new FtpResponse(550, $"\"{sMessage}\" no such directory.");
            }

            ConnectionObject.CurrentDirectory = newDirectory;
            return new FtpResponse(250, $"{Command} Successful ({newDirectory})");
        }
    }
}