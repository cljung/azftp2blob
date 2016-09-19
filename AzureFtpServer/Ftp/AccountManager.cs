using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AzureFtpServer.Provider;

namespace AzureFtpServer.Ftp
{
    class FtpAccount
    {
        public string Userid { get; set; }
        public string Password { get; set; }
        public string RootFolder { get; set; }
        public bool ReadAccess { get; set; }
        public bool WriteAccess { get; set; }

        public override string ToString()
        {
            return Userid;
        }
    }
    /// <summary>
    /// AccountManager Class
    /// Read account information from config settings and store valid (username,password) pairs
    /// </summary>
    class AccountManager
    {
        #region Member Variables

        private const char separator = ';';
        private readonly Dictionary<string, FtpAccount> _accounts;
        private int _usernum;
        
        #endregion

        #region Construction

        public AccountManager()
        {
            //_accounts = new Dictionary<string, string>();
            _accounts = new Dictionary<string, FtpAccount>();
            _usernum = 0;
        }

        #endregion

        #region Properties
        
        public int UserNum 
        {
            get { return _usernum; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Read the settings in RoleEnvironment, insert into the accounts dictionary
        /// </summary>
        /// <returns></returns>
        public int LoadConfigration()
        {
            // init member vars 
            _usernum = 0;
            _accounts.Clear();

            string filename = StorageProviderConfiguration.FtpAccount;
            StreamReader sr = new StreamReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            //string[] accountInfo = StorageProviderConfiguration.FtpAccount.Split(":".ToCharArray());

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if (line.Length == 0)
                    continue;
                if (line.StartsWith("#"))
                    continue;

                string[] accountInfo = line.Split(":".ToCharArray());

                for (int i = 0; i < accountInfo.Length; i++)
                {
                    string oneAccount = accountInfo[i];
                    string[] parts = oneAccount.Split(separator);

                    if (parts.Length < 3)
                    {
                        Trace.WriteLine(string.Format("Invalid account data. Must be userid;pwd;folder", oneAccount), "Warnning");
                        continue;
                    }

                    // get the username substr
                    string username = parts[0].ToLowerInvariant();

                    // check the username whether conform to the naming rules
                    if (!CheckUsername(username))
                    {
                        Trace.WriteLine(string.Format("Invalid username in account data", oneAccount), "Warnning");
                        continue;
                    }

                    // check whether the username already exists
                    if (_accounts.ContainsKey(username))
                    {
                        continue;
                    }

                    // get the password substr
                    string password = parts[1];
                    // simple check, password can not be empty
                    if (password.Length == 0)
                    {
                        Trace.WriteLine(string.Format("Invalid password in account data", oneAccount), "Warnning");
                        continue;
                    }

                    // get the password substr
                    string rootFolder = parts[2];
                    // simple check, password can not be empty
                    if (rootFolder.Length == 0)
                    {
                        Trace.WriteLine(string.Format("Invalid folder in account data", oneAccount), "Warnning");
                        continue;
                    }

                    FtpAccount account = new FtpAccount
                    {
                        Userid = username,
                        Password = password,
                        RootFolder = rootFolder,
                        ReadAccess = true,
                        WriteAccess = true
                    };

                    if (parts.Length > 3)
                    {
                        string rights = parts[3].ToLower();
                        account.WriteAccess = rights.IndexOf('w') != -1;
                        account.ReadAccess = account.WriteAccess || rights.IndexOf('r') != -1;
                    }

                    _accounts.Add(username, account);
                    _usernum++;
                }
            }
            sr.Close();


            Trace.WriteLine($"Load {_usernum} accounts.", "Information");
            
            return _usernum;
        }

        /// <summary>
        /// Checks if (username, password) is a valid account
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool CheckAccount(string username, string password, out FtpAccount account)
        {
            username = username.ToLowerInvariant();
            if (!_accounts.TryGetValue(username, out account))
            {
                return false;
            }

            if (account.Password == password)
            {
                return true;
            }

            account = null;
            return false;

        }

        /// <summary>
        /// checks whether username conform to the Azure container naming rules
        /// 1. start with a letter or number, and can contain only letters, numbers, and the dash (-) character
        /// 2. Every dash (-) character must be immediately preceded and followed by a letter or number; 
        ///    consecutive dashes are not permitted in container names.
        /// 3. All letters in a container name must be lowercase.
        /// 4. Container names must be from 3 through 63 characters long.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private bool CheckUsername(string username)
        {
            if (!Regex.IsMatch(username, @"^\$root$|^[a-z0-9]([a-z0-9]|(?<=[a-z0-9])-(?=[a-z0-9])){2,62}$"))
                return false;

            return true;
        }

        #endregion
    }
}
