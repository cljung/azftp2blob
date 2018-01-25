using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// PASS command handler
    /// get password and do login
    /// </summary>
    internal class PasswordCommandHandler : FtpCommandHandler
    {
        public PasswordCommandHandler(FtpConnectionObject connectionObject)
            : base("PASS", connectionObject)
        {
        }

        protected override string OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
                return GetMessage(501, string.Format("{0} needs a parameter", Command));

            if (ConnectionObject.Login(sMessage))
            {
                FtpServer.LogWrite(this, "******", 220, 0);
                return GetMessage(230, "Password ok, FTP server ready");
            }
            else
            {
                FtpServer.LogWrite(this, "******", 530, 0);
                return GetMessage(530, "Username or password incorrect");
            }
        }
    }
}