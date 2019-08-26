using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// LIST command handler
    /// list detailed information about files/directories
    /// </summary>
    internal class ListCommandHandler : ListCommandHandlerBase
    {
        public ListCommandHandler(FtpConnectionObject connectionObject)
            : base("LIST", connectionObject)
        {
        }

        protected override string BuildReply(IFileInfo[] asFiles, IFileInfo[] asDirectories)
        {
            return BuildLongReply(asFiles, asDirectories);
        }
    }
}