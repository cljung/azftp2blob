using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// NLST command handler
    /// name list
    /// </summary>
    internal class NlstCommandHandler : ListCommandHandlerBase
    {
        public NlstCommandHandler(FtpConnectionObject connectionObject)
            : base("NLST", connectionObject)
        {
        }

        protected override string BuildReply(IFileInfo[] asFiles, IFileInfo[] asDirectories)
        {
            return BuildShortReply(asFiles, asDirectories);
        }
    }
}