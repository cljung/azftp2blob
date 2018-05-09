using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFtpServer.Ftp
{
    class FtpCommandException : Exception
    {
        public string MessageToClient { get; }

        internal FtpCommandException(string errorMsg, string messageToClient)
            : base(errorMsg)
        {
            MessageToClient = messageToClient;
        }
    }
}
