using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// SMNT command handler, superfluous at this site
    /// </summary>
    internal class SmntCommandHandler : FtpCommandHandler
    {
        public SmntCommandHandler(FtpConnectionObject connectionObject)
            : base("SMNT", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            return new FtpResponse(202, "");
        }
    }
}