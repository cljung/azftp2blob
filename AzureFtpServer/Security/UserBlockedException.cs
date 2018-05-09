using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureFtpServer.Ftp;

namespace AzureFtpServer.Security
{
    class UserBlockedException : FtpCommandException
    {
        internal UserBlockedException(string message, string reply)
            : base(message, reply)
        {
        }
    }
}
