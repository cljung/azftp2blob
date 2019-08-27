using AzureFtpServer.Ftp;
using AzureFtpServer.Security;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// PASS command handler
    /// get password and do login
    /// </summary>
    internal class PasswordCommandHandler : FtpCommandHandler
    {
        private readonly InvalidAttemptCounter invalidLoginCounter;

        public PasswordCommandHandler(FtpConnectionObject connectionObject, InvalidAttemptCounter invalidLoginCounter)
            : base("PASS", connectionObject)
        {
            this.invalidLoginCounter = invalidLoginCounter;
        }

        public override bool CanLogCommandArg => false;

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
            {
                return new FtpResponse(501, $"{Command} needs a parameter");
            }

            if (ConnectionObject.Login(sMessage))
            {
                invalidLoginCounter.OnSuccesfullLogin(ConnectionObject.User);
                return new FtpResponse(230, "Password ok, FTP server ready");
            }

            invalidLoginCounter.OnInvalidLogin(ConnectionObject.User);
            return new FtpResponse(530, "Username or password incorrect");
        }
    }
}