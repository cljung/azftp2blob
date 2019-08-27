using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// STOU command handler, superfluous at this site
    /// Store unique
    /// </summary>
    internal class StouCommandHandler : FtpCommandHandler
    {
        public StouCommandHandler(FtpConnectionObject connectionObject)
            : base("STOU", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            return new FtpResponse(202, "Use STOR instead");
        }
    }
}