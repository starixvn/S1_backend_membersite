using MemberSite.Extensions;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class PayInfoBLL
    {
        private readonly PayService _payService;
        private readonly DepositService _depositService;
        private readonly DepositBLL _depositBll;
        private readonly BankInfoOffBLL _bankInfoOffBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly ICacheHelper _redisHelper;

        public PayInfoBLL(IServiceProvider provider)
        {
            _payService = provider.GetRequiredService<PayService>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositService = provider.GetRequiredService<DepositService>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _bankInfoOffBll = provider.GetRequiredService<BankInfoOffBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }



        public IEnumerable<PayGroupInfo> GetPayGroupWithCache()
        {
            var dbData = _redisHelper.TryGetOrCreate("PayGroupInfoList", TimeSpan.FromMinutes(5),
                () => _payService.GetPayGroup(), RedisDataBase.Shared);
            return dbData;
        }

        private Dictionary<DepositType, OfflineBankAccountSupportChannelEnum> _dicSupportChannelMapper
            = new()
            {
                {DepositType.OnlineBanking, OfflineBankAccountSupportChannelEnum.LocalBank},
                {DepositType.MobilePayment, OfflineBankAccountSupportChannelEnum.EWallet},
                {DepositType.QRPayPayment, OfflineBankAccountSupportChannelEnum.BankQR}
            };

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
                .Where(info => vipLevel >= info.PointLevel && info.CurrencyId == custInfo.CurrencyId);

            var payGroupList = payInfoList
                .Select(x => new PayGroup
                {
                    GroupId = x.GroupId,
                    GroupName = x.GroupName,
                    ChannelName = x.ChannelName,
                    GSort = x.GSort,
                    Mode = x.Mode,
                    MinAmount = payInfoList.Where(y => x.GroupId == y.GroupId).OrderBy(y => y.MinAmount).First().MinAmount,
                    MaxAmount = payInfoList.Where(y => x.GroupId == y.GroupId).OrderByDescending(y => y.MaxAmount).First().MaxAmount
                })
                .Distinct(i => i.GroupId)
                .ToList();

            var bankCards = _bankInfoOffBll.GetOfflineBankCardListWithCache(custId)
                .Where(x => _dicSupportChannelMapper.ContainsKey(depositType)
                            && x.SupportChannel.Contains(_dicSupportChannelMapper[depositType]));

            if (bankCards.Any())
            {
                var payGroupInfos = GetPayGroupWithCache();
                var payGroups = bankCards
                    .SelectMany(b => b.GroupIds,
                        (b, g) => new
                        {
                            GroupId = g,
                            b.MinAmount,
                            b.MaxAmount
                        })
                    .GroupBy(x => x.GroupId)
                    .Where(x => payGroupInfos.Any(g => g.GroupId == x.Key && g.Type == depositType))
                    .Select(x =>
                    {
                        var bankCard = x.First();
                        var groupInfo = payGroupInfos.First(g => g.GroupId == x.Key);
                        return new PayGroup
                        {
                            GroupId = bankCard.GroupId,
                            ChannelName = groupInfo.GroupName,
                            GroupName = groupInfo.GroupName,
                            Mode = (int)PayModeEnum.Redirect,
                            GSort = groupInfo.Sort,
                            MinAmount = bankCards.Min(x => x.MinAmount),
                            MaxAmount = bankCards.Max(x => x.MaxAmount)
                        };
                    });

                foreach (var payGroup in payGroups)
                {
                    var g = payGroupList.FirstOrDefault(x => x.GroupId == payGroup.GroupId);
                    if (g == null)
                    {
                        payGroupList.Add(payGroup);
                        continue;
                    }

                    g.MaxAmount = payGroup.MaxAmount > g.MaxAmount ? payGroup.MaxAmount : g.MaxAmount;
                    g.MinAmount = payGroup.MinAmount < g.MinAmount ? payGroup.MinAmount : g.MinAmount;
                }
            }

            if (depositLimitInfo != null)
            {
                foreach (var payGroup in payGroupList)
                {
                    _depositBll.FixDepositLimit(payGroup, depositLimitInfo);
                }
            }

            return payGroupList.OrderBy(x => x.GSort);
        }

        public IEnumerable<PayInfo> GetPayInfoList(int custId, int groupId, DepositType depositType)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = GetDepositLimitInfo(custInfo, vipLevel, depositType);
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<PayInfo>();
            }

            var payInfoList = GetPayInfoList(custInfo.SiteId, depositType)
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId)
                .ToList();

            var bankCards = _bankInfoOffBll.GetOfflineBankCardListWithCache(custId)
                .Where(x => _dicSupportChannelMapper.ContainsKey(depositType)
                            && x.SupportChannel.Contains(_dicSupportChannelMapper[depositType])
                            && x.GroupIds.Contains(groupId));

            if (bankCards.Any())
            {
                var payGroupInfos = GetPayGroupWithCache();
                var groupInfo = payGroupInfos.First(g => g.GroupId == groupId);
                var payInfo = new PayInfo
                {
                    ChannelId = -1,
                    ChannelName = groupInfo.GroupName,
                    GSort = groupInfo.Sort,
                    SSort = -1,
                    Mode = (int)PayModeEnum.Redirect,
                    GroupId = groupId,
                    GroupName = groupInfo.GroupName,
                    MinAmount = bankCards.Min(x => x.MinAmount),
                    MaxAmount = bankCards.Max(x => x.MaxAmount)
                };

                payInfoList.Add(payInfo);
            }

            if (depositLimitInfo != null && payInfoList != null)
            {
                foreach (var payInfo in payInfoList)
                {
                    _depositBll.FixDepositLimit(payInfo, depositLimitInfo);
                }
            }

            return payInfoList.Where(x => x.MaxAmount > 0)
                .OrderBy(x => x.GSort)
                .ThenBy(x => x.SSort);
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
            var key = $"PaySetting:{siteId}:{depositType}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<PayInfo>>(key, TimeSpan.FromMinutes(5),
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
        
        public IEnumerable<WithdrawalBlockSetting> GetWithdrawalBlockSettingWithCache(int siteId, int currencyId)
        {
            var key = $"PaySetting:WithdrawalBlock:{siteId}";
            
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<WithdrawalBlockSetting>>(key, TimeSpan.FromMinutes(5),
                () => _payService.GetWithdrawalBlockSettingList(siteId),
                RedisDataBase.Shared);
            
            return dbData.Where(x => x.CurrencyId == currencyId);
        }
        
    }
}
