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

        protected override string OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();
            if (sMessage == "")
                return GetMessage(501, string.Format("{0} needs a parameter", Command));

            string fileToDelete = GetPath(sMessage);
            Trace.TraceInformation(string.Format("DELE {0} - BEGIN", fileToDelete));
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            // 2015-11-24 cljung : Q&D fix. If path contains double slashes, reduce to single since
            //                     NTFS/etc treams sub1//sub2 as sub1/sub two but Azure Blob Storage doesn't
            if (ConnectionObject.FileSystemObject.FileExists(fileToDelete) )
            {
                if (!StorageProviderConfiguration.FtpReplaceSlashOnDELE)
                    fileToDelete = fileToDelete.Replace("//", "/");
                else
                {
                    FtpServer.LogWrite(this, sMessage, 550, sw.ElapsedMilliseconds);
                    return GetMessage(550, string.Format("File \"{0}\" does not exist.", fileToDelete));
                }
            }

            if (!ConnectionObject.FileSystemObject.FileExists(fileToDelete))
            {
                FtpServer.LogWrite(this, sMessage, 550, sw.ElapsedMilliseconds);
                return GetMessage(550, string.Format("File \"{0}\" does not exist.", fileToDelete));
            }

            if (!ConnectionObject.FileSystemObject.DeleteFile(fileToDelete))
            {
                FtpServer.LogWrite(this, sMessage, 550, sw.ElapsedMilliseconds);
                return GetMessage(550, string.Format("Delete file \"{0}\" failed.", fileToDelete));
            }
            sw.Stop();
            Trace.TraceInformation(string.Format("DELE {0} - END, Time {1} ms", fileToDelete, sw.ElapsedMilliseconds));

            FtpServer.LogWrite(this, sMessage, 250, sw.ElapsedMilliseconds);
            return GetMessage(250, string.Format("{0} successful. Time {1} ms", Command, sw.ElapsedMilliseconds));
        }
    }
}