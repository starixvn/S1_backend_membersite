using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class LoadCardPayInfoBLL
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositBLL _depositBll;
        private readonly PayService _payService;
        private readonly DepositService _depositService;
        private readonly ICacheHelper _redisHelper;
        
        public LoadCardPayInfoBLL(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _payService = provider.GetRequiredService<PayService>();
            _depositService = provider.GetRequiredService<DepositService>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        private IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int siteId)
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

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?
                .Where(x => x.SiteId == custInfo.SiteId
                            && x.VipLevel == pointLevel
                            && x.DepositType == (int)DepositType.LoadCardPayment
                            && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

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
                    _depositBll.FixDepositLimit(payGroup, depositLimitInfo);
                }
            }

            return LoadCardPayGroupList;
        }

        public IEnumerable<LoadCardPayInfo> GetLoadCardPayInfoList(int custId, int groupId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.MobilePayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new LoadCardPayInfo[] { };
            }

            var LoadCardPayInfoList = GetLoadCardPayInfoList(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId).ToList();

            if (depositLimitInfo != null && LoadCardPayInfoList != null)
            {
                foreach (var LoadCardPayInfo in LoadCardPayInfoList)
                {
                    _depositBll.FixDepositLimit(LoadCardPayInfo, depositLimitInfo);
                }
            }

            return LoadCardPayInfoList.Where(x => x.MaxAmount > 0);
        }
        public int DepositReqLoadCardPay(int custId, int channelId, string code, string serial, decimal amount, out string transId, out LoadCardPayInfo loadCardPayInfo)
        {
            transId = null;
            loadCardPayInfo = null;
            if (amount <= 0)
                return -1;
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?
                .Where(x => x.SiteId == custInfo.SiteId
                            && x.VipLevel == pointLevel
                            && x.DepositType == (int)DepositType.LoadCardPayment
                            && x.CurrencyId == custInfo.CurrencyId)
                .FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            loadCardPayInfo = GetLoadCardPayInfoList(custInfo.SiteId)
                .FirstOrDefault(x => pointLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId);

            if (depositLimitInfo != null && loadCardPayInfo != null)
            {
                _depositBll.FixDepositLimit(loadCardPayInfo, depositLimitInfo);
            }

            if (loadCardPayInfo == null || loadCardPayInfo.MaxAmount < amount || loadCardPayInfo.MinAmount > amount)
            {
                return -2;
            }
            
            if (loadCardPayInfo.AmountRule == 3 && !loadCardPayInfo.AmountList.Contains(amount))
            {
                return -2;
            }

            if (loadCardPayInfo != null)
            {
                loadCardPayInfo.Code = code;
                loadCardPayInfo.Serial = serial;

                var depositDetailInfo = new DepositDetailInfo
                {
                    Amount = amount,
                    CustId = custId,
                    DepositType = (int)DepositType.LoadCardPayment,
                    Memo = null,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId,
                    DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(loadCardPayInfo)
                };

                _depositService.DepositReq(depositDetailInfo, out transId, out _);
                return 0;
            }

            return -99;
        }
    }
}
