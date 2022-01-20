using System;
using System.IO;

namespace AzureFtpServer.Ftp.FileSystem
{
    public interface IFile
    {
        Stream BlobStream { get; set; }
        int Read(byte[] abData, int nDataSize);
        int Write(byte[] abData, int nDataSize);
        void Close();
    }

    public interface IFileInfo
    {
        DateTime GetModifiedTime();
        long GetSize();
        string GetAttributeString();
        bool IsDirectory();
        string Path();
        bool FileObjectExists();
    }

    public interface IFileSystem
    {
        IFile OpenFile(string sPath, bool fWrite);
        IFileInfo GetFileInfo(string sPath);
        IFileInfo GetDirectoryInfo(string sPath);

        IFileInfo[] GetFiles(string sDirPath);
        IFileInfo[] GetDirectories(string sDirPath, bool actualCreationTime);

        bool DirectoryExists(string sDirPath);
        bool FileExists(string sPath);

        bool CreateDirectory(string sPath);
        bool Move(string sOldPath, string sNewPath);// file, not directory
        bool DeleteFile(string sPath);
        bool DeleteDirectory(string sPath);
        bool AppendFile(string sPath, Stream stream);

        void Log4Upload(string sPath);// upload notification
    }

    public interface IFileSystemClassFactory
    {
        IFileSystem Create(string sUser, string sPassword);
    }
}