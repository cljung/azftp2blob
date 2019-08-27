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

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
            {
                return new FtpResponse(501, $"{Command} needs a parameter");
            }

            if (!invalidLoginCounter.IsUserAllowed(sMessage))
            {
                var clientMsg = new FtpResponse(421, "Service not available, closing control connection");
                throw new UserBlockedException($"user {sMessage} is blocked due to excessive invalid logon attempts", clientMsg.ToString());
            }

            ConnectionObject.User = sMessage;
            return new FtpResponse(331, $"User {sMessage} logged in, needs password");
        }
    }
}