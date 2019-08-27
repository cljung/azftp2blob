using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// SIZE command handler
    /// return the size of a file in ftp server
    /// </summary>
    internal class SizeCommandHandler : FtpCommandHandler
    {
        public SizeCommandHandler(FtpConnectionObject connectionObject)
            : base("SIZE", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            string sPath = GetPath(sMessage);

            if (!ConnectionObject.FileSystemObject.FileExists(sPath))
            {
                return new FtpResponse(550, $"File doesn't exist ({sPath})");
            }

            IFileInfo info = ConnectionObject.FileSystemObject.GetFileInfo(sPath);

            if (info == null)
            {
                return new FtpResponse(550, "Error in getting file information");
            }

            return new FtpResponse(220, info.GetSize().ToString());
        }
    }
}