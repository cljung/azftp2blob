using System.Diagnostics;
using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// PORT command handler
    /// enter active mode
    /// </summary>
    internal class PortCommandHandler : FtpCommandHandler
    {
        public PortCommandHandler(FtpConnectionObject connectionObject)
            : base("PORT", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            string[] asData = sMessage.Split(new[] {','});

            if (asData.Length != 6)
            {
                return new FtpResponse(501, $"{Command}: Syntax error in parameters");
            }

            ConnectionObject.DataConnectionType = DataConnectionType.Active;

            int nSocketPort = int.Parse(asData[4])*256 + int.Parse(asData[5]);

            ConnectionObject.PortCommandSocketPort = nSocketPort;
            ConnectionObject.PortCommandSocketAddress = string.Join(".", asData, 0, 4);

            return new FtpResponse(200, $"{Command} successful.");
        }
    }
}