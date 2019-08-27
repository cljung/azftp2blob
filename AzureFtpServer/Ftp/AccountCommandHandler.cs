using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// ACCT command manger
    /// No need for this ftp server
    /// </summary>
    internal class AccountCommandHandler : FtpCommandHandler
    {
        public AccountCommandHandler(FtpConnectionObject connectionObject)
            : base("ACCT", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            // TODO: stop current service & close data connection
            return new FtpResponse(230, "Account information not needed");
        }
    }
}