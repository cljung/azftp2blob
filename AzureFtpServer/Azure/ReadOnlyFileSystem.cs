using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;

namespace AzureFtpServer.Azure
{
    public sealed class ReadOnlyFileSystem : IFileSystem
    {
        private readonly IFileSystem inner;
        public IFile OpenFile(string sPath, bool fWrite)
        {
            if (fWrite)
            {
                FailOnReadOnlyFs(sPath);
                return null;
            }

            return new ReadOnlyFile(inner.OpenFile(sPath, false));
        }

        public IFileInfo GetFileInfo(string sPath)
        {
            return inner.GetFileInfo(sPath);
        }

        public IFileInfo GetDirectoryInfo(string sPath)
        {
            return inner.GetDirectoryInfo(sPath);
        }

        public IFileInfo[] GetFiles(string sDirPath)
        {
            return inner.GetFiles(sDirPath);
        }

        public IFileInfo[] GetDirectories(string sDirPath)
        {
            return inner.GetDirectories(sDirPath);
        }

        public bool DirectoryExists(string sDirPath)
        {
            return inner.DirectoryExists(sDirPath);
        }

        public bool FileExists(string sPath)
        {
            return inner.FileExists(sPath);
        }

        public bool CreateDirectory(string sPath) => FailOnReadOnlyFs(sPath);

        public bool Move(string sOldPath, string sNewPath) => FailOnReadOnlyFs(sOldPath);

        public bool DeleteFile(string sPath) => FailOnReadOnlyFs(sPath);

        public bool DeleteDirectory(string sPath) => FailOnReadOnlyFs(sPath);

        public bool AppendFile(string sPath, Stream stream) => FailOnReadOnlyFs(sPath);

        public void Log4Upload(string sPath)
        {
            inner.Log4Upload(sPath);
        }

        public ReadOnlyFileSystem(IFileSystem inner)
        {
            this.inner = inner;
        }

        private bool FailOnReadOnlyFs(string path = null, [CallerMemberName] string caller = null)
        {
            FtpServer.LogWrite($"attempt to {caller} {path} on read only file system");
            return false;
        }
    }
}
