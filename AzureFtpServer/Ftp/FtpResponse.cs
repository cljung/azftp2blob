using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFtpServer.Ftp
{
    /// <summary>
    /// Represents single ftp response
    /// </summary>
    public sealed class FtpResponse
    {
        public int Code { get; }
        public string Message { get; }

        public FtpResponse(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString()
        {
            if (Code == 0)
            {
                return Message;
            }
            return $"{Code} {Message}\r\n";
        }
    }
}
