using MemberSite.Extensions;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class MobilePayInfoBll
    {
        private readonly PayService _payService;
        private readonly DepositService _depositService;
        private readonly DepositBLL _depositBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly BankInfoOffBLL _bankInfoOffBll;
        private readonly ICacheHelper _redisHelper;
        public MobilePayInfoBll(IServiceProvider provider)
        {
            _payService = provider.GetRequiredService<PayService>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositService = provider.GetRequiredService<DepositService>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _bankInfoOffBll = provider.GetRequiredService<BankInfoOffBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        public IEnumerable<MobilePayInfo> GetMobilePayInfoList(int siteId)
        {
            var key = $"PaySetting:MobilePay:{siteId}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<MobilePayInfo>>(key, TimeSpan.FromMinutes(5),
                () => _payService.GetMobilePayInfo(siteId).OrderBy(x => x.GSort).ThenBy(x => x.SSort));
            return dbData;
        }

        public IEnumerable<MobilePayGroup> GetMobilePayGroupList(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var mobilePayInfoList = GetMobilePayInfoList(custInfo.SiteId)
                .Where(x => pointLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?
                .Where(x => x.SiteId == custInfo.SiteId
                            && x.VipLevel == pointLevel
                            && x.DepositType == (int)DepositType.MobilePayment
                            && x.CurrencyId == custInfo.CurrencyId)
                .FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<MobilePayGroup>();
            }

            var mobilePayGroupList = mobilePayInfoList
                .GroupBy(x => x.GroupId)
                .Select(x =>
                {
                    var d = x.OrderBy(y => y.SSort).First();
                    var m = mobilePayInfoList.Where(y => y.ChannelId == d.ChannelId);

                    return new MobilePayGroup
                    {
                        GroupId = d.GroupId,
                        GroupName = d.GroupName,
                        ChannelName = d.ChannelName,
                        GSort = d.GSort,
                        Mode = d.Mode,
                        MinAmount = m.Min(y => y.MinAmount),
                        MaxAmount = m.Max(y => y.MaxAmount)
                    };
                }).ToList();
                
            if (custInfo.CurrencyId == (int) CurrencyId.MYR)
            {
                var bankCards = _bankInfoOffBll.GetOfflineBankCardListWithCache(custId)
                    .Where(x => x.SupportQR);

                if (bankCards.Any())
                {
                    mobilePayGroupList.Add(new MobilePayGroup
                    {
                        ChannelName = FakePayGroupEnum.DuitNowQR.GetDescription(),
                        GroupId = (int) FakePayGroupEnum.DuitNowQR,
                        GroupName = FakePayGroupEnum.DuitNowQR.ToString(),
                        Mode = (int) PayModeEnum.QrCode,
                        GSort = -1,
                        MinAmount = bankCards.Min(x => x.MinAmount),
                        MaxAmount = bankCards.Max(x => x.MaxAmount)
                    });
                }
            }
            
            return mobilePayGroupList.OrderBy(x => x.GSort);
        }
        
        public IEnumerable<MobilePayInfo> GetMobilePayInfoList(int custId, int groupId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.MobilePayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new MobilePayInfo[] { };
            }

            var mobilePayInfoList = GetMobilePayInfoList(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId).ToList();

            if (depositLimitInfo != null && mobilePayInfoList != null)
            {
                foreach (var mobilePayInfo in mobilePayInfoList)
                {
                    _depositBll.FixDepositLimit(mobilePayInfo, depositLimitInfo);
                }
            }

            return mobilePayInfoList.Where(x => x.MaxAmount > 0);
        }

        public int DepositReqMobilePay(int custId, int channelId, decimal amount, out string transId, out MobilePayInfo mobilePayInfo)
        {
            transId = null;
            mobilePayInfo = null;
            if (amount <= 0)
                return -1;
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.MobilePayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            mobilePayInfo = GetMobilePayInfoList(custInfo.SiteId).FirstOrDefault(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId);

            if (depositLimitInfo != null && mobilePayInfo != null)
            {
                _depositBll.FixDepositLimit(mobilePayInfo, depositLimitInfo);
            }

            if (mobilePayInfo == null || mobilePayInfo.MaxAmount < amount || mobilePayInfo.MinAmount > amount)
            {
                return -2;
            }
            else if (mobilePayInfo.AmountRule == 3 && !mobilePayInfo.AmountList.Contains(amount))
            {
                return -2;
            }

            if (mobilePayInfo != null)
            {
                var depositDetailInfo = new DepositDetailInfo
                {
                    Amount = amount,
                    CustId = custId,
                    DepositType = (int)DepositType.MobilePayment,
                    Memo = null,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId,
                    DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(mobilePayInfo)
                };

                _depositService.DepositReq(depositDetailInfo, out transId, out _);
                return 0;
            }

            return -99;
        }
    }
}
