using System;
using System.Runtime.Remoting.Messaging;
using System.Text;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.Provider;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureFtpServer.Azure
{
    public sealed class AzureFileInfo : IFileInfo
    {
        public AzureFileInfo(AzureCloudFile file)
        {
            exists = file != null;
            if (exists)
            {
                path = file.FtpPath;
                lastModified = file.LastModified;
                size = file.Size;
                isDirectory = file.IsDirectory;
            }
        }

        private readonly CloudBlockBlob blob;
        public AzureFileInfo(CloudBlockBlob blob)
        {
            this.blob = blob;

            exists = blob != null;
            if (exists)
            {
                path = blob.Name;
                isDirectory = false;
                lastModified = blob.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow;
                size = blob.Properties.Length;
            }
        }

        public AzureFileInfo(CloudBlobDirectory blob)
        {
            exists = blob != null;
            if (exists)
            {
                path = blob.Uri.ToString().Replace(blob.Container.Uri.ToString(), string.Empty);
                isDirectory = true;
                lastModified = DateTime.Now;
                size = 1;
            }
        }

        #region Implementation of IFileInfo 

        private readonly bool exists;
        public bool FileObjectExists()
        {
            return exists;
        }

        private readonly string path;
        public string Path()
        {
            return path;
        }

        private readonly DateTime lastModified;
        public DateTime GetModifiedTime()
        {
            return lastModified;
        }

        private readonly long size;
        public long GetSize()
        {
            return size;
        }

        private readonly bool isDirectory;
        public bool IsDirectory()
        {
            return isDirectory;
        }

        public string GetAttributeString()
        {
            bool fDirectory = IsDirectory();
            bool fReadOnly = false; // No file should be read-only.

            var builder = new StringBuilder();

            builder.Append(fDirectory ? "d" : "-");

            builder.Append("r");

            if (fReadOnly)
            {
                builder.Append("-");
            }
            else
            {
                builder.Append("w");
            }

            builder.Append(fDirectory ? "x" : "-");

            builder.Append(fDirectory ? "r-xr-x" : "r--r--");

            return builder.ToString();
        }

        #endregion
    }
}