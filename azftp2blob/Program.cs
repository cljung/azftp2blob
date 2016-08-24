using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureFtpServer.Azure;
using AzureFtpServer.Ftp;
using AzureFtpServer.Provider;

// https://ftp2azure.codeplex.com/SourceControl/latest

namespace azftp2blob
{
    class Program
    {
        private FtpServer _server;
        private bool _verbose = true;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Program p = new Program();
            p.Go(args);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            FtpServer.LogWrite($"UNHANDLED ERROR: {e.ExceptionObject}");
        }

        private int FindArg(string[] args, string argument)
        {
            if (argument.StartsWith("/") || argument.StartsWith("-"))
                argument = argument.Substring(1);
            for (int n = 0; n < args.Length; n++)
            {
                string arg = args[n];
                if (arg.StartsWith("/") || arg.StartsWith("-"))
                    arg = arg.Substring(1);
                if (string.Compare(arg, argument, true) == 0)
                    return n;
            }
            return -1;
        }
        private string GetArgument(string[] args, string key, string defaultValue)
        {
            // get key/value from A) App.Config, B) env var, C) command line
            string value = ConfigurationManager.AppSettings[key];
            string buf = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(buf))
                value = buf;
            int idx;
            if (-1 != (idx = FindArg(args, key)))
            {
                value = args[idx + 1];
            }
            if (value == null)
                value = defaultValue;
            return value;
        }
        private bool GetArgsAndConfig(string[] args)
        {
            bool rc = false;
            _verbose = GetArgument(args, "verbose", "false").ToLowerInvariant() == "true";
            return rc;
        }
        public void Go(string[] args)
        {
            GetArgsAndConfig(args);
            if (_verbose)
            {
                ConsoleTraceListener conTrace = new ConsoleTraceListener();
                Trace.Listeners.Add(conTrace);
            }
            _server = new FtpServer(new AzureFileSystemFactory());
            _server.NewConnection += ServerNewConnection;
            _server.Start();
        }
        static void ServerNewConnection(int nId)
        {
            Console.WriteLine($"Connection {nId} accepted", "Connection");
        }
    }
}
