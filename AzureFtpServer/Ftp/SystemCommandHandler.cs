﻿using System;
using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// SYST command handler
    /// show the system version of this ftp server
    /// </summary>
    internal class SystemCommandHandler : FtpCommandHandler
    {
        public SystemCommandHandler(FtpConnectionObject connectionObject)
            : base("SYST", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            return new FtpResponse(215, Environment.OSVersion.VersionString);
        }
    }
}