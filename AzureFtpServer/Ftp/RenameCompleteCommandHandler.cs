using System.Diagnostics;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// RNTO command handler
    /// </summary>
    internal class RenameCompleteCommandHandler : FtpCommandHandler
    {
        public RenameCompleteCommandHandler(FtpConnectionObject connectionObject)
            : base("RNTO", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            if (ConnectionObject.FileToRename.Length == 0)
            {
                return new FtpResponse(503, "RNTO must be preceded by a RNFR.");
            }

            string sOldFileName = ConnectionObject.FileToRename;
            ConnectionObject.FileToRename = ""; // note: 

            sMessage = sMessage.Trim();
            string sNewFileName = GetPath(sMessage);
            // check whether the new filename is valid
            if (!FileNameHelpers.IsValid(sNewFileName) || sNewFileName.EndsWith(@"/"))
            {
                return new FtpResponse(553, $"\"{sMessage}\" is not a valid file name");
            }
            
            // check whether the new filename exists
            // note: azure allows file&virtualdir has the same name
            if (ConnectionObject.FileSystemObject.FileExists(sNewFileName))
            {
                return new FtpResponse(553, $"File already exists ({sMessage}).");
            }

            if (!ConnectionObject.FileSystemObject.Move(sOldFileName, sNewFileName))
            {
                return new FtpResponse(553, "Rename failed");
            }

            return new FtpResponse(250, "Renamed file successfully.");
        }
    }
}