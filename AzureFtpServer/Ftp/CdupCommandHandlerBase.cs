using System.Diagnostics;
using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// base class for CDUP & XCDP command handlers
    /// </summary>
    internal class CdupCommandHandlerBase : FtpCommandHandler
    {
        public CdupCommandHandlerBase(string sCommand, FtpConnectionObject connectionObject)
            : base(sCommand, connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            if (sMessage.Length != 0)
            {
                return new FtpResponse(501, $"Invalid syntax for {Command} command");
            }

            // get the parent directory
            string parentDir = GetParentDir();
            if (parentDir == null)
            {
                return new FtpResponse(550, "Root directory, cannot change to the parent directory");
            }
            ConnectionObject.CurrentDirectory = parentDir;
            return new FtpResponse(200, $"{Command} Successful ({parentDir})");
        }
    }
}