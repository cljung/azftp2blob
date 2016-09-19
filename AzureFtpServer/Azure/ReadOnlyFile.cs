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
    class ReadOnlyFile : IFile
    {
        private readonly IFile inner;
        public Stream BlobStream
        {
            get { return inner.BlobStream; }
            set { inner.BlobStream = value; }
        }

        public int Read(byte[] abData, int nDataSize)
        {
            return inner.Read(abData, nDataSize);
        }

        public int Write(byte[] abData, int nDataSize)
        {
            FailOnReadOnlyFs();
            return -1;
        }

        public void Close()
        {
            inner.Close();
        }

        public ReadOnlyFile(IFile inner)
        {
            this.inner = inner;
        }

        private bool FailOnReadOnlyFs([CallerMemberName] string caller = null)
        {
            FtpServer.LogWrite($"attempt to {caller} on read only file");
            return false;
        }
    }
}
