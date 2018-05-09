using AzureFtpServer.Ftp;
using AzureFtpServer.Security;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// USER command handler
    /// set username
    /// </summary>
    internal class UserCommandHandler : FtpCommandHandler
    {
        private readonly InvalidAttemptCounter invalidLoginCounter;

        public UserCommandHandler(FtpConnectionObject connectionObject, InvalidAttemptCounter invalidLoginCounter)
            : base("USER", connectionObject)
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

            if (!invalidLoginCounter.IsUserAllowed(sMessage))
            {
                FtpServer.LogWrite(this, sMessage, 421, 0);
                string clientMsg = GetMessage(421, "Service not available, closing control connection");
                throw new UserBlockedException($"user {sMessage} is blocked due to excessive invalid logon attempts", clientMsg);
            }

            ConnectionObject.User = sMessage;

            FtpServer.LogWrite(this, sMessage, 331, 0);
            return GetMessage(331, $"User {sMessage} logged in, needs password");
        }
    }
}