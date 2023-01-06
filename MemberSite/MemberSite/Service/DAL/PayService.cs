using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Library;

namespace MemberSite.Service.DAL
{
    public class PayService
    {
        private readonly MySqlClient _reportDbClient;
        
        public PayService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public IEnumerable<MobilePayInfo> GetMobilePayInfo(int siteId)
        {
            return _reportDbClient.Query<MobilePayInfo>("Member_MobilePay_Get_V3", new { _SiteId = siteId });
        }
        
        public IEnumerable<CryptoPayInfo> GetCryptoPayInfo(int siteId)
        {
            return _reportDbClient.Query<CryptoPayInfo>("Service_PayInfo_Get_V2", new { _SiteId = siteId, _DepositType = DepositType.CryptoPayment });
        }
        
        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfo(int siteId)
        {
            return _reportDbClient.Query<LoadCardPayInfo>("Service_PayInfo_Get_V2", new { _SiteId = siteId, _DepositType = DepositType.LoadCardPayment });
        }

        public IEnumerable<PayInfo> GetPayInfo(int siteId, DepositType depositType)
        {
            return _reportDbClient.Query<PayInfo>("Service_PayInfo_Get_V2", new { _SiteId = siteId , _DepositType = (int)depositType });
        }

        public LineChannel? GetLineChannel(int siteId, int currencyId)
        {
            return _reportDbClient.QueryFirst<LineChannel?>("Member_LineChannel_Get", new { _SiteId = siteId, _CurrencyId = currencyId });
        }
    }
}
