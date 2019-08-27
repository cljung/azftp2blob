﻿using System.Text;
using AzureFtpServer.General;
using AzureFtpServer.Ftp;
using AzureFtpServer.Ftp.General;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// MLST command handler
    /// only list file/directory info
    /// </summary>
    class MlstCommandHandler : MlsxCommandHandlerBase
    {
        public MlstCommandHandler(FtpConnectionObject connectionObject)
            : base("MLST", connectionObject)
        { 
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            sMessage = sMessage.Trim();

            // Get the file/dir to list
            string targetToList = GetPath(sMessage);

            // checks the file/dir name
            if (!FileNameHelpers.IsValid(targetToList))
            {
                return new FtpResponse(501, $"\"{sMessage}\" is not a valid file/directory name");
            }

            bool targetIsFile = ConnectionObject.FileSystemObject.FileExists(targetToList);
            bool targetIsDir = ConnectionObject.FileSystemObject.DirectoryExists(FileNameHelpers.AppendDirTag(targetToList));

            if (!targetIsFile && !targetIsDir)
            {
                return new FtpResponse(550, $"\"{sMessage}\" not exists");
            }

            SocketHelpers.Send(ConnectionObject.Socket, $"250- MLST {targetToList}\r\n", ConnectionObject.Encoding);

            StringBuilder response = new StringBuilder();

            if (targetIsFile)
            {
                response.Append(" ");
                var fileInfo = ConnectionObject.FileSystemObject.GetFileInfo(targetToList);
                response.Append(GenerateEntry(fileInfo));
                response.Append("\r\n");
            }
            
            if (targetIsDir)
            {
                response.Append(" ");
                var dirInfo = ConnectionObject.FileSystemObject.GetDirectoryInfo(FileNameHelpers.AppendDirTag(targetToList));
                response.Append(GenerateEntry(dirInfo));
                response.Append("\r\n");
            }

            SocketHelpers.Send(ConnectionObject.Socket, response.ToString(), ConnectionObject.Encoding);

            return new FtpResponse(250, "MLST successful");
        }
    }
}
