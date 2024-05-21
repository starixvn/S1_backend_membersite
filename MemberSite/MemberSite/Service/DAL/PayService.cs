using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class PayService
    {
        private readonly MySqlClient _reportDbClient;

        public PayService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<PayInfo> GetPayInfo(int siteId, DepositType? depositType, string lang = "")
        {
            return _reportDbClient.Query<PayInfo>("Service_PayInfo_Get_V3", new
            {
                _SiteId = siteId,
                _DepositType = (int?)depositType ?? 0,
                _Lang = lang
            });
        }

        public LineChannel? GetLineChannel(int siteId, int currencyId)
        {
            return _reportDbClient.QueryFirst<LineChannel?>("Member_LineChannel_Get", new { _SiteId = siteId, _CurrencyId = currencyId });
        }

        public IEnumerable<PayGroupInfo> GetPayGroup(string lang)
        {
            return _reportDbClient.Query<PayGroupInfo>("Service_PayGroup_Get_V2", new
            {
                _Lang = lang
            });
        }

        public IEnumerable<PayChannelInfo> GetPayChannel()
        {
            return _reportDbClient.Query<PayChannelInfo>("Service_PayChannel_Get");
        }

        public IEnumerable<WithdrawalBlockSetting> GetWithdrawalBlockSettingList(int siteId)
        {
            return _reportDbClient.Query<WithdrawalBlockSetting>("Service_WithdrawalBlockSetting_Get", new { _SiteId = siteId });
        }
    }
}
