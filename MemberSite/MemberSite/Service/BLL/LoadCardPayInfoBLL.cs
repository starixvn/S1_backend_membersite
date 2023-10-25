using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class LoadCardPayInfoBLL
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositLimitBLL _depositLimitBll;
        private readonly PayService _payService;
        private readonly DepositService _depositService;
        private readonly ICacheHelper _redisHelper;

        public LoadCardPayInfoBLL(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositLimitBll = provider.GetRequiredService<DepositLimitBLL>();
            _payService = provider.GetRequiredService<PayService>();
            _depositService = provider.GetRequiredService<DepositService>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int siteId)
        {
            var key = $"PaySetting:LoadCard:{siteId}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<LoadCardPayInfo>>(key, TimeSpan.FromMinutes(5),
                () => _payService.GetLoadCardPayInfo(siteId).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
        }

        public IEnumerable<LoadCardPayGroup> GetLoadCardPayGroupList(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var LoadCardPayInfoList = GetLoadCardPayInfoList(custInfo.SiteId);
            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.LoadCardPayment);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<LoadCardPayGroup>();
            }

            var LoadCardPayGroupList = LoadCardPayInfoList
                .Where(x => pointLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId)
                .GroupBy(x => x.GroupId)
                .Select(x =>
                {
                    var d = x.OrderBy(y => y.SSort).First();

                    return new LoadCardPayGroup
                    {
                        GroupId = d.GroupId,
                        GroupName = d.GroupName,
                        PayName = d.PayName,
                        GSort = d.GSort,
                        Mode = d.Mode,
                        MaxAmount = d.MaxAmount,
                        MinAmount = d.MinAmount
                    };
                });

            if (depositLimitInfo != null)
            {
                foreach (var payGroup in LoadCardPayGroupList)
                {
                    _depositLimitBll.FixDepositLimit(payGroup, depositLimitInfo);
                }
            }

            return LoadCardPayGroupList;
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int custId, int groupId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.LoadCardPayment);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new LoadCardPayInfo[] { };
            }

            var LoadCardPayInfoList = GetLoadCardPayInfoList(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId).ToList();

            if (depositLimitInfo != null && LoadCardPayInfoList != null)
            {
                foreach (var LoadCardPayInfo in LoadCardPayInfoList)
                {
                    _depositLimitBll.FixDepositLimit(LoadCardPayInfo, depositLimitInfo);
                }
            }

            return LoadCardPayInfoList.Where(x => x.MaxAmount > 0);
        }
    }
}
