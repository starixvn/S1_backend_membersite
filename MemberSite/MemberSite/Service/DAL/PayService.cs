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

        public IEnumerable<PayInfo> GetPayInfo(int siteId, DepositType? depositType)
        {
            return _reportDbClient.Query<PayInfo>("Service_PayInfo_Get_V2", new
            {
                _SiteId = siteId,
                _DepositType = (int?)depositType ?? 0
            });
        }

        public LineChannel? GetLineChannel(int siteId, int currencyId)
        {
            return _reportDbClient.QueryFirst<LineChannel?>("Member_LineChannel_Get", new { _SiteId = siteId, _CurrencyId = currencyId });
        }

        public IEnumerable<PayGroupInfo> GetPayGroup()
        {
            return _reportDbClient.Query<PayGroupInfo>("Service_PayGroup_Get");
        }

        public IEnumerable<PayChannelInfo> GetPayChannel()
        {
            return _reportDbClient.Query<PayChannelInfo>("Service_PayChannel_Get");
        }
        
        public IEnumerable<PayGroupName> GetPayGroupName()
        {
            return _reportDbClient.Query<PayGroupName>("Member_PayGroupName_Get");
        }

        public IEnumerable<PayChannelName> GetPayChannelName()
        {
            return _reportDbClient.Query<PayChannelName>("Member_PayChannelName_Get");
        }

        public IEnumerable<WithdrawalBlockSetting> GetWithdrawalBlockSettingList(int siteId)
        {
            return _reportDbClient.Query<WithdrawalBlockSetting>("Service_WithdrawalBlockSetting_Get", new { _SiteId = siteId });
        }
    }
}
