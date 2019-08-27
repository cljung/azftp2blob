using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// STRU command handler
    /// show the data structure of this connection
    /// </summary>
    internal class StructureCommandHandler : FtpCommandHandler
    {
        public StructureCommandHandler(FtpConnectionObject connectionObject)
            : base("STRU", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            switch (sMessage.ToUpper())
            {
                case "F":
                    ConnectionObject.DataStructure = DataStructure.File;
                    return new FtpResponse(200, $"{Command} command succeeded, Structure is File");
                case "R":
                case "P":
                    ConnectionObject.DataStructure = DataStructure.File;
                    return new FtpResponse(504, $"Data structure {sMessage} is not supported, use File Structure");
                default:
                    return new FtpResponse(501, $"Error - Unknown data structure \"{sMessage}\"");
            }
        }
    }
}