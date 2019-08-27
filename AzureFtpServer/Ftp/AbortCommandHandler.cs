using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// ABOR command handler
    /// abort current data connection, TODO
    /// </summary>
    internal class AbortCommandHandler : FtpCommandHandler
    {
        public AbortCommandHandler(FtpConnectionObject connectionObject)
            : base("ABOR", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            // TODO: stop current service & close data connection
            return new FtpResponse(226, "Current data connection aborted");
        }
    }
}