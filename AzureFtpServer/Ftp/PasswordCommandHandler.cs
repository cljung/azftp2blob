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

        protected override string OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
            {
                return GetMessage(501, $"{Command} needs a parameter");
            }

            if (ConnectionObject.Login(sMessage))
            {
                FtpServer.LogWrite(this, "******", 230, 0);
                invalidLoginCounter.OnSuccesfullLogin(ConnectionObject.User);
                return GetMessage(230, "Password ok, FTP server ready");
            }

            FtpServer.LogWrite(this, "******", 530, 0);
            invalidLoginCounter.OnInvalidLogin(ConnectionObject.User);
            return GetMessage(530, "Username or password incorrect");
        }
    }
}