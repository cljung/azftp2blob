using AzureFtpServer.Ftp.FileSystem;
using AzureFtpServer.Ftp;

namespace AzureFtpServer.Azure
{
    public class AzureFileSystemFactory : IFileSystemClassFactory
    {
        #region Member variables
        private AccountManager m_accountManager;
        #endregion

        #region Construction
        public AzureFileSystemFactory()
        {
            m_accountManager = new AccountManager();
            m_accountManager.LoadConfigration();
        }
        #endregion

        #region Implementation of IFileSystemClassFactory

        public IFileSystem Create(string sUser, string sPassword)
        {
            if ((sUser == null) || (sPassword == null))
            {
                return null;
            }

            FtpAccount account;
            if (!m_accountManager.CheckAccount(sUser, sPassword, out account))
            {
                return null;
            }

            if (!account.WriteAccess && !account.ReadAccess)
            {
                FtpServer.LogWrite($"user {sUser} has neither read nor write permissions");
                return null;
            }

            IFileSystem system = new AzureFileSystem(account.RootFolder ?? sUser);
            if (!account.WriteAccess)
            {
                system = new ReadOnlyFileSystem(system);
            }

            return system;
        }

        #endregion
    }
}