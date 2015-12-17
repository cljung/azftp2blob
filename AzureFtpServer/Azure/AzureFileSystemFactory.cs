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
                return null;

            string containerName = sUser;
            if (!m_accountManager.CheckAccount(sUser, sPassword, out containerName))
                return null;
            
            var system = new AzureFileSystem(containerName);
            
            return system;
        }

        #endregion
    }
}