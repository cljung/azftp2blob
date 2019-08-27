using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// MODE command handler
    /// show the transmission mode of this connection
    /// </summary>
    internal class ModeCommandHandler : FtpCommandHandler
    {
        public ModeCommandHandler(FtpConnectionObject connectionObject)
            : base("MODE", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            switch (sMessage.ToUpper())
            {
                case "S":
                    ConnectionObject.TransmissionMode = TransmissionMode.Stream;
                    return new FtpResponse(200, $"{Command} command succeeded, transmission mode is Stream");
                case "B":
                case "C":
                    ConnectionObject.TransmissionMode = TransmissionMode.Stream;
                    return new FtpResponse(504, $"Transfer mode {sMessage} is not supported, use Stream Mode");
                default:
                    return new FtpResponse(501, $"Error - Unknown transimmsion mode \"{sMessage}\"");
            }
        }
    }
}