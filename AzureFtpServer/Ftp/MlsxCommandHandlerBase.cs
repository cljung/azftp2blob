using System.Text;
using AzureFtpServer.Azure;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.General;
using AzureFtpServer.General;
using AzureFtpServer.Ftp.FileSystem;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// Base class for MLST & MLSD command handlers
    /// </summary>
    internal class MlsxCommandHandlerBase : FtpCommandHandler
    {
        protected MlsxCommandHandlerBase(string sCommand, FtpConnectionObject connectionObject)
            : base(sCommand, connectionObject)
        {
        }

        protected string GenerateEntry(IFileInfo info)
        {
            StringBuilder entry = new StringBuilder();

            bool isDirectory = info.IsDirectory();

            if (isDirectory)
            {
                entry.Append(
                    $"Type=dir;Size={info.GetSize()};Modify={info.GetModifiedTime():yyyyMMddHHmmss}; ");
                string dirName = FileNameHelpers.GetDirectoryName(info.Path().ToFtpPath());
                entry.Append(dirName);
            }
            else
            {
                entry.Append(
                    $"Type=file;Size={info.GetSize()};Modify={info.GetModifiedTime():yyyyMMddHHmmss}; ");
                entry.Append(FileNameHelpers.GetFileName(info.Path().ToFtpPath()));
            }

            return entry.ToString();
        }
        
    }
}