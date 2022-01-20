using System.Text;
using System.Diagnostics;
using System.Configuration;
using AzureFtpServer.Ftp;
using AzureFtpServer.Provider;


namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// DELE command handler
    /// delete a file
    /// </summary>
    internal class DeleCommandHandler : FtpCommandHandler
    {
        public DeleCommandHandler(FtpConnectionObject connectionObject)
            : base("DELE", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
            {
                return new FtpResponse(501, $"{Command} needs a parameter");
            }

            string fileToDelete = GetPath(sMessage);
            Trace.TraceInformation($"DELE {fileToDelete} - BEGIN");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // 2015-11-24 cljung : Q&D fix. If path contains double slashes, reduce to single since
            //                     NTFS/etc treams sub1//sub2 as sub1/sub two but Azure Blob Storage doesn't
            if (!StorageProviderConfiguration.FtpReplaceSlashOnDELE)
            {
                fileToDelete = fileToDelete.Replace("//", "/");
            }

            if (!ConnectionObject.FileSystemObject.FileExists(fileToDelete))
            {
                return new FtpResponse(550, $"File \"{fileToDelete}\" does not exist.");
            }

            if (!ConnectionObject.FileSystemObject.DeleteFile(fileToDelete))
            {
                return new FtpResponse(550, $"Delete file \"{fileToDelete}\" failed.");
            }
            sw.Stop();
            Trace.TraceInformation($"DELE {fileToDelete} - END, Time {sw.ElapsedMilliseconds} ms");

            return new FtpResponse(250, $"{Command} successful. Time {sw.ElapsedMilliseconds} ms");
        }
    }
}