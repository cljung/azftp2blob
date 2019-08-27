using AzureFtpServer.Ftp;

namespace AzureFtpServer.FtpCommands
{
    /// <summary>
    /// TYPE command handler
    /// set the data type of this connection
    /// </summary>
    internal class TypeCommandHandler : FtpCommandHandler
    {
        public TypeCommandHandler(FtpConnectionObject connectionObject)
            : base("TYPE", connectionObject)
        {
        }

        protected override FtpResponse OnProcess(string sMessage)
        {
            string[] args = sMessage.Split(' ');
            
            if (args.Length > 2 || args.Length < 1)
            {
                return new FtpResponse(501, $"Invalid TYPE parameter: {sMessage}");
            }

            switch (args[0].ToUpper())
            { 
                case "A":
                    ConnectionObject.DataType = DataType.Ascii;
                    if (args.Length == 1)
                    {
                        return new FtpResponse(200, $"{Command} command succeeded, data type is Ascii");
                    }
                    else 
                    {
                        switch (args[1].ToUpper())
                        { 
                            case "N":
                                ConnectionObject.FormatControl = FormatControl.NonPrint;
                                return new FtpResponse(200, $"{Command} command succeeded, data type is Ascii, format is NonPrint");
                            case "T":
                            case "C":
                                ConnectionObject.FormatControl = FormatControl.NonPrint;
                                return new FtpResponse(504, $"Format {args[1]} is not supported, use NonPrint format");
                            default:
                                return new FtpResponse(550, $"Error - unknown format \"{args[1]}\"");
                        }
                    }
                case "I":
                    ConnectionObject.DataType = DataType.Image;
                    return new FtpResponse(200, $"{Command} command succeeded, data type is Image (Binary)");
                case "E":
                case "L":
                    ConnectionObject.DataType = DataType.Image;
                    return new FtpResponse(504, $"Data type {args[0]} is not supported, use Image (Binary) type");
                default:
                    return new FtpResponse(550, $"Error - unknown data type \"{args[1]}\"");
            }
        }
    }
}