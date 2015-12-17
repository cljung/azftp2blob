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

        protected override string OnProcess(string sMessage)
        {
            string sDirectory = ConnectionObject.CurrentDirectory;
            FtpServer.LogWrite(this, sMessage, 257, 0);
            return GetMessage(257, string.Format("\"{0}\" {1} Successful.", sDirectory, Command));
        }
    }
}