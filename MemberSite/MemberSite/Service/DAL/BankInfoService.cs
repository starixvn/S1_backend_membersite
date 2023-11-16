using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class BankInfoService
    {
        private readonly MySqlClient _reportDb;

        public BankInfoService(IServiceProvider provider)
        {
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public IEnumerable<BankInfoSys> GetBankInfoSysList(int siteId)
        {
            return _reportDb.Query<BankInfoSys>("Member_BankInfoSysList_Get", new { SiteId = siteId });
        }

        public IEnumerable<BankInfoSys> GetBankInfoSysListForCtrl(int siteId)/*Filter Out BankInfoSys.Status != 0*/
        {
            //siteId Still Useless
            return _reportDb.Query<BankInfoSys>("BankInfoSysListForCtrl_Get", new { SiteId = siteId });
        }

        public IEnumerable<BankInfoPay> GetBankInfoPayList(int siteId, DepositType depositType)
        {
            return _reportDb.Query<BankInfoPay>("Common_BankInfoPay_Get_V4", new { _SiteId = siteId, _DepositType = (int)depositType });
        }

        public IEnumerable<BankInfoName> GetBankInfoNameList(string lang)
        {
            return _reportDb.Query<BankInfoName>("Member_BankInfoName_Get", new { _Lang = lang });
        }

        public IEnumerable<BlockAccount> GetBlockBankAccount(int sysId, string accountNum)
        {
            return _reportDb.Query<BlockAccount>("Member_BlockAccount_Get", new { SysId = sysId, AccountNum = accountNum });
        }

        public IEnumerable<EWalletInfoSys> GetEWalletInfoSysList()
        {
            //siteId Still Useless
            return _reportDb.Query<EWalletInfoSys>("Service_EWalletInfoSys_Get");
        }

        public IEnumerable<BlockAccount> GetBlockEWalletAccount(int sysId, string accountNum)
        {
            return _reportDb.Query<BlockAccount>("Member_BlockEWallet_Get", new { SysId = sysId, AccountNum = accountNum });
        }

        public IEnumerable<BlockCryptoWallet> GetBlockCryptoWallet(string address)
        {
            return _reportDb.Query<BlockCryptoWallet>("Member_BlockCryptoWallet_Get", new { Address = address });
        }
    }
}
