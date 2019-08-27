using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// REST command handler
    /// </summary>
    internal class RestartCommandHandler : FtpCommandHandler
    {
        public RestartCommandHandler(FtpConnectionObject connectionObject)
            : base("REST", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            return new FtpResponse(500, "Restart not supported!");
        }
    }
}