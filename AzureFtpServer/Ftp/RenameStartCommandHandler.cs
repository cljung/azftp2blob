using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// RNFR command handler
    /// Starts a rename file operation
    /// </summary>
    internal class RenameStartCommandHandler : FtpCommandHandler
    {
        public RenameStartCommandHandler(FtpConnectionObject connectionObject)
            : base("RNFR", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();

            if (sMessage.Length == 0)
            {
                return new FtpResponse(501, "Syntax error. RNFR needs a parameter");
            }

            string sFile = GetPath(sMessage);

            // check whether file exists
            if (!ConnectionObject.FileSystemObject.FileExists(sFile))
            {
                return new FtpResponse(550, $"File {sMessage} not exists. Rename directory not supported.");
            }

            ConnectionObject.FileToRename = sFile;

            return new FtpResponse(350, $"Rename file started ({sFile}).");
        }
    }
}