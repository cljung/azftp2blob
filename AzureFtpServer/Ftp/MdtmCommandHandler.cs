using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// MDTM command handler
    /// show last modified time of files
    /// </summary>
    internal class MdtmCommandHandler : FtpCommandHandler
    {
        public MdtmCommandHandler(FtpConnectionObject connectionObject)
            : base("MDTM", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            string sPath = GetPath(sMessage.Trim());

            if (!ConnectionObject.FileSystemObject.FileExists(sPath))
            {
                return new FtpResponse(550, $"File doesn't exist ({sPath})");
            }

            IFileInfo info = ConnectionObject.FileSystemObject.GetFileInfo(sPath);

            if (info == null)
            {
                return new FtpResponse(550, "Error in getting file information");
            }

            return new FtpResponse(213, info.GetModifiedTime().ToString());
        }
    }
}
