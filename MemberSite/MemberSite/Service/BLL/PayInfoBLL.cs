using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using MemberSite.Service.Library;
using MemberSite.Extensions;

namespace MemberSite.Service.BLL
{
    public class PayInfoBLL
    {
        private readonly PayService _payService;
        private readonly DepositService _depositService;
        private readonly DepositBLL _depositBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly ICacheHelper _redisHelper;

        public PayInfoBLL(IServiceProvider provider)
        {
            _payService = provider.GetRequiredService<PayService>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositService = provider.GetRequiredService<DepositService>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public IEnumerable<PayGroup> GetPayGroupList(int custId, DepositType depositType)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = GetDepositLimitInfo(custInfo, vipLevel, depositType);
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new List<PayGroup>();
            }

            var payInfoList = GetPayInfoList(custInfo.SiteId, depositType)
                .Where(info=> vipLevel >= info.PointLevel && info.CurrencyId == custInfo.CurrencyId);

            return payInfoList
                .Select(x => new PayGroup()
                {
                    GroupId = x.GroupId,
                    GroupName = x.GroupName,
                    ChannelName = x.ChannelName,
                    GSort = x.GSort,
                    Mode = x.Mode,
                    MinAmount = payInfoList.Where(y => x.GroupId == y.GroupId).OrderBy(y => y.MinAmount).First().MinAmount,
                    MaxAmount = payInfoList.Where(y => x.GroupId == y.GroupId).OrderByDescending(y => y.MaxAmount).First().MaxAmount
                })
                .Distinct(i => i.GroupId);
        }

        public IEnumerable<PayInfo> GetPayInfoList(int custId, int groupId, DepositType depositType)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = GetDepositLimitInfo(custInfo, vipLevel, depositType);
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new List<PayInfo>();
            }

            var payInfoList = GetPayInfoList(custInfo.SiteId, depositType)
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId);

            if (depositLimitInfo != null && payInfoList != null)
            {
                foreach (var payInfo in payInfoList)
                {
                    _depositBll.FixDepositLimit(payInfo, depositLimitInfo);
                }
            }

            return payInfoList.Where(x => x.MaxAmount > 0);
        }

        public int DepositReqPay(int custId, int channelId, decimal amount, out string transId, out PayInfo payInfo, DepositType depositType)
        {
            transId = null;
            payInfo = null;
            if (amount <= 0)
            {
                return -1;
            }
            
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = GetDepositLimitInfo(custInfo, vipLevel, depositType);
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            payInfo = GetPayInfoList(custInfo.SiteId, depositType)
                .FirstOrDefault(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId);

            if (depositLimitInfo != null && payInfo != null)
            {
                _depositBll.FixDepositLimit(payInfo, depositLimitInfo);
            }

            if (payInfo == null || payInfo.MaxAmount < amount || payInfo.MinAmount > amount)
            {
                return -2;
            }
            else if (payInfo.AmountRule == 3 && !payInfo.AmountList.Contains(amount))
            {
                return -2;
            }


            var depositDetailInfo = new DepositDetailInfo
            {
                Amount = amount,
                CustId = custId,
                DepositType = (int)depositType,
                Memo = null,
                SiteId = custInfo.SiteId,
                UserName = custInfo.UserName,
                CurrencyId = custInfo.CurrencyId,
                DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(payInfo)
            };

            _depositService.DepositReq(depositDetailInfo, out transId, out _);
            return 0;      
        }

        private IEnumerable<PayInfo> GetPayInfoList(int siteId, DepositType depositType)
        {
            var key = $"PayInfoList:{siteId}:{depositType}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<PayInfo>>(key, TimeSpan.FromSeconds(10),
                () => _payService.GetPayInfo(siteId, depositType).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
        }

        private DepositLimitInfo GetDepositLimitInfo(Customer custInfo, int vipLevel, DepositType depositType)
        {
            return _depositBll.GetDepositLimitInfoList(custInfo.SiteId)?
                    .FirstOrDefault(x =>
                                    x.SiteId == custInfo.SiteId &&
                                    x.VipLevel == vipLevel &&
                                    x.DepositType == (int)depositType &&
                                    x.CurrencyId == custInfo.CurrencyId);
        }
    }
}
