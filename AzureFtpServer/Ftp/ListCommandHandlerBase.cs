using System;
using System.Text;
using System.Diagnostics;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.Ftp.General;
using AzureFtpServer.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// Base class for LIST/NLST command handlers
    /// </summary>
    internal abstract class ListCommandHandlerBase : FtpCommandHandler
    {
        public ListCommandHandlerBase(string sCommand, FtpConnectionObject connectionObject)
            : base(sCommand, connectionObject)
        {
        }

        protected override string OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();

            IFileInfo[] asFiles = null;
            IFileInfo[] asDirectories = null;

            // Get the file/dir to list
            string targetToList = GetPath(sMessage);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // checks the file/dir name
            if (!FileNameHelpers.IsValid(targetToList))
            {
                FtpServer.LogWrite(this, sMessage, 501, 0);
                return GetMessage(501, string.Format("\"{0}\" is not a valid file/directory name", sMessage));
            }

            // two vars indicating different list results
            bool targetIsFile = false;
            bool targetIsDir = false;

            // targetToList ends with '/', must be a directory
            if (targetToList.EndsWith(@"/"))
            {
                targetIsFile = false;
                if (ConnectionObject.FileSystemObject.DirectoryExists(targetToList))
                    targetIsDir = true;
            }
            else
            {
                // check whether the target to list is a directory
                if (ConnectionObject.FileSystemObject.DirectoryExists(FileNameHelpers.AppendDirTag(targetToList)))
                    targetIsDir = true;
                // check whether the target to list is a file
                if (ConnectionObject.FileSystemObject.FileExists(targetToList))
                    targetIsFile = true;
            }

            if (targetIsFile)
            {
                asFiles = new IFileInfo[] { ConnectionObject.FileSystemObject.GetFileInfo(targetToList) };
                if (targetIsDir)
                    asDirectories = new IFileInfo[] { ConnectionObject.FileSystemObject.GetDirectoryInfo(targetToList) };
            }
            // list a directory
            else if (targetIsDir)
            {
                targetToList = FileNameHelpers.AppendDirTag(targetToList);
                asFiles = ConnectionObject.FileSystemObject.GetFiles(targetToList);
                asDirectories = ConnectionObject.FileSystemObject.GetDirectories(targetToList);
            }
            else 
            {
                FtpServer.LogWrite(this, sMessage, 550, sw.ElapsedMilliseconds);
                return GetMessage(550, string.Format("\"{0}\" not exists", sMessage));
            }

            var socketData = new FtpDataSocket(ConnectionObject);

            if (!socketData.Loaded)
            {
                FtpServer.LogWrite(this, sMessage, 425, sw.ElapsedMilliseconds);
                return GetMessage(425, "Unable to establish the data connection");
            }

            // prepare to write response to data channel
            SocketHelpers.Send(ConnectionObject.Socket, string.Format("150 Opening data connection for {0}\r\n", Command), ConnectionObject.Encoding);

            // generate the response
            string sFileList = BuildReply(asFiles, asDirectories);

            // ToDo, send response according to ConnectionObject.DataType, i.e., Ascii or Binary
            socketData.Send(sFileList, Encoding.UTF8);
            socketData.Close();

            sw.Stop();
            FtpServer.LogWrite(this, sMessage, 226, sw.ElapsedMilliseconds);
            return GetMessage(226, string.Format("{0} successful.", Command));
        }

        protected abstract string BuildReply(IFileInfo[] asFiles, IFileInfo[] asDirectories);

        // build short list reply, only list the file names
        protected string BuildShortReply(IFileInfo[] asFiles, IFileInfo[] asDirectories)
        {
            var stringBuilder = new StringBuilder();

            if (asFiles != null)
            {
                foreach (var file in asFiles)
                {
                    stringBuilder.Append($"{FileNameHelpers.GetFileName(file.Path())}\r\n");
                }
            }

            if (asDirectories != null)
            {
                foreach (var dir in asDirectories)
                {
                    stringBuilder.Append($"{FileNameHelpers.GetDirectoryName(dir.Path())}\r\n");
                }
            }
            
            return stringBuilder.ToString();
        }

        // build detailed list reply
        protected string BuildLongReply(IFileInfo[] asFiles, IFileInfo[] asDirectories)
        {
            var stringBuilder = new StringBuilder();

            if (asFiles != null)
            {
                foreach (IFileInfo fileInfo in asFiles)
                {
                    stringBuilder.Append(GetLongProperty(fileInfo));
                }
            }

            if (asDirectories != null)
            {
                foreach (IFileInfo dirInfo in asDirectories)
                {
                    stringBuilder.Append(GetLongProperty(dirInfo));
                }
            }

            return stringBuilder.ToString();
        }

        private string GetLongProperty(IFileInfo info)
        {
            if (info == null)
                return null;

            var stringBuilder = new StringBuilder();

            // permissions
            string sAttributes = info.GetAttributeString();
            stringBuilder.Append(sAttributes);
            stringBuilder.Append(" 1 owner group");

            // check whether info is directory
            bool isDirectory = info.IsDirectory();

            // size
            string sFileSize = info.GetSize().ToString(); // if info is directory, the size will be 1
            stringBuilder.Append(TextHelpers.RightAlignString(sFileSize, 13, ' '));
            stringBuilder.Append(" ");

            // modify time
            DateTime fileDate = info.GetModifiedTime(); //if info is directory, the modify time will be the current time
            // month
            stringBuilder.Append(TextHelpers.Month(fileDate.Month));
            stringBuilder.Append(" ");
            // day
            string sDay = fileDate.Day.ToString();
            if (sDay.Length == 1)
                stringBuilder.Append(" ");
            stringBuilder.Append(sDay);
            stringBuilder.Append(" ");
            // year or hour:min
            if (fileDate.Year < DateTime.Now.Year)
            {
                stringBuilder.Append(" " + fileDate.Year);
            }
            else 
            {
                stringBuilder.Append(string.Format("{0:hh}:{1:mm}", fileDate, fileDate));
            }
            stringBuilder.Append(" ");

            // filename
            string path = info.Path();
            if (isDirectory)
                stringBuilder.Append(FileNameHelpers.GetDirectoryName(path));
            else
                stringBuilder.Append(FileNameHelpers.GetFileName(path));

            // end
            stringBuilder.Append("\r\n");

            return stringBuilder.ToString();
        }
    }
}