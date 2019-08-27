using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// Base class for PWD & XPWD command handlers
    /// </summary>
    internal class PwdCommandHandlerBase : FtpCommandHandler
    {
        public PwdCommandHandlerBase(string sCommand, FtpConnectionObject connectionObject)
            : base(sCommand, connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            string sDirectory = ConnectionObject.CurrentDirectory;
            return new FtpResponse(257, $"\"{sDirectory}\" {Command} Successful.");
        }
    }
}