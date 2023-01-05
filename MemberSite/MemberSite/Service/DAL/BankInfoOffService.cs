using MemberSite.Service.Model;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class BankInfoOffService
    {
        private readonly MySqlClient _reportDbClient;

        public BankInfoOffService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId, int queryType = 1)
        {
            if (queryType == 0)
                return _reportDbClient.Query<BankInfoOff>("BankAccountOff_Get", new { SiteId = siteId });
            else
                return _reportDbClient.Query<BankInfoOff>("Member_BankAccountOffAllStatus_Get", new { SiteId = siteId });
        }
        
        public IEnumerable<OfflineBankAccountInfo> GetOfflineBankAccount(int siteId)
        {
            return _reportDbClient.Query<OfflineBankAccountInfo>("Member_OfflineBankAccount_Get", new { _SiteId = siteId });
        }
        
        public IEnumerable<int> GetCustOffDepositSysId(int custId)
        {
            return _reportDbClient.Query<int>("Member_CustOffDepositSysId_Get", new { _CustId = custId });
        }
    }
}
