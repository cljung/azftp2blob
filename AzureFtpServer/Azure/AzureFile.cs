using System;
using System.IO;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;


namespace AzureFtpServer.Azure
{
    public sealed class AzureFile : IFile
    {
        private readonly string name;

        #region Implementation of IFile

        public AzureFile(string name)
        {
            this.name = name;
        }


        public Stream BlobStream { get; set; }

        public int Read(byte[] abData, int nDataSize)
        {
            if (BlobStream == null)
            {
                return 0;
            }

            try
            {
                return BlobStream.Read(abData, 0, nDataSize);
            }
            catch (IOException io)
            {
                FtpServer.LogWrite($"IO failure when reading {name}: {io}");
                return 0;
            }
            // other exceptions
            catch (Exception e)
            {
                FtpServer.LogWrite($"error while reading file {name}: {e}");
                // need logging, fix me
                return 0;
            }
        }

        public int Write(byte[] abData, int nDataSize)
        {
            if (BlobStream == null)
            {
                throw new Exception("BlobStream is null!");
            }

            try
            {
                BlobStream.Write(abData, 0, nDataSize);
                return nDataSize;
            }
            catch (IOException io)
            {
                FtpServer.LogWrite($"IO failure when writing {name}: {io}");
                return 0;
            }
            catch (Exception e)
            {
                FtpServer.LogWrite($"failed to write to {name}: {e}");
                return 0;
            }
        }

        public void Close()
        {
            if (BlobStream != null)
            {
                try
                {
                    if (BlobStream.CanWrite)
                    {
                        BlobStream.Flush();
                    }
                    BlobStream.Close();
                }
                catch (IOException)
                {
                }

                BlobStream = null;
            }
        }

        #endregion
    }
}