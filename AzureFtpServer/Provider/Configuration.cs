using System;
using System.Configuration;
using System.Runtime.CompilerServices;
using Microsoft.WindowsAzure.Storage;

namespace AzureFtpServer.Provider
{
    public enum Modes
    {
        Live,
        Debug
    }

    public class StorageProviderConfiguration
    {
        
        public static string FtpAccount
        {
            get
            {
                return ConfigurationManager.AppSettings["FtpAccount"];
            }
        }

        public static Modes Mode
        {
            get
            {
                return (Modes)Enum.Parse(typeof(Provider.Modes), ConfigurationManager.AppSettings["Mode"]);
            }
        }

        public static bool QueueNotification
        {
            get
            {
                return bool.Parse(ConfigurationManager.AppSettings["QueueNotification"]);
            }
        }

        public static int MaxIdleSeconds
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["MaxIdleSeconds"]);
            }
        }

        public static string FtpServerHost
        {
            get
            {
                string buf = ConfigurationManager.AppSettings["FtpServerHost"];
                buf = Environment.ExpandEnvironmentVariables(buf);
                return buf;
            }
        }
        public static string FtpServerHostPublic
        {
            get
            {
                string buf = ConfigurationManager.AppSettings["FtpServerHostPublic"];
                buf = Environment.ExpandEnvironmentVariables(buf);
                return buf;
            }
        }

        public static bool FtpOverwriteFileOnSTOR => CheckFlag();

        public static bool FtpReplaceSlashOnDELE => CheckFlag();
        public static bool FtpActualDirectoryCreationTime => CheckFlag();

        private static bool CheckFlag([CallerMemberName] string key = null)
        {
            string buf = ConfigurationManager.AppSettings[key];
            return !string.IsNullOrEmpty(buf) && buf.ToLowerInvariant() == "true";
        }
        
    }
}