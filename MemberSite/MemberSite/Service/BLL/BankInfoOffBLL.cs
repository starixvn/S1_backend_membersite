using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using System.Text.Json;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class BankInfoOffBLL
    {
        private readonly BankInfoOffService _bankInfoService;
        private readonly BankInfoBLL _bankInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositBLL _depositBll;
        private readonly SysBankInfoOffComparer _sysBankInfoOffComparer;
        private readonly DepositService _depositService;
        private readonly ICacheHelper _redisHelper;
        private readonly ILogger<BankInfoOffBLL> _logger;

        public BankInfoOffBLL(IServiceProvider provider)
        {
            _bankInfoService = provider.GetRequiredService<BankInfoOffService>();
            _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _sysBankInfoOffComparer = new SysBankInfoOffComparer();
            _depositService = provider.GetRequiredService<DepositService>();
            _logger = provider.GetRequiredService<ILogger<BankInfoOffBLL>>();
        }

        public IEnumerable<BankInfoOff> GetBankAccountOff(int siteId, int queryType = 1)
        {
            return _bankInfoService.GetBankAccountOff(siteId, queryType).Where(x => (x.Purpose & 1) == 1);
        }
        
        /// <summary>
        /// 取代 GetBankAccountOff
        /// </summary>
        public IEnumerable<OfflineBankAccountInfo> GetOfflineBankAccount(int siteId)
        {
            return _bankInfoService.GetOfflineBankAccount(siteId)
                .Where(x => x.Purposes.Contains(OfflineBankAccountPurposeEnum.Deposit));
        }

        private class SysBankInfoOffComparer : IEqualityComparer<SysBankInfoFront>
        {
            public bool Equals(SysBankInfoFront t1, SysBankInfoFront t2)
            {
                return (t1.SysId == t2.SysId && t1.BankCode == t2.BankCode);
            }

            public int GetHashCode(SysBankInfoFront t)
            {
                return t.ToString().GetHashCode();
            }
        }
        
        public IEnumerable<SysBankInfoFront> GetOfflineBankCardListWithCache(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var siteId = custInfo.SiteId;
            var currencyId = custInfo.CurrencyId;

            var key = $"GetOfflineBankCardList:{siteId}:{currencyId}:{pointLevel}:{custInfo.UserLevel}";
            var offBankList = _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => GetOfflineBankAccountList(siteId, currencyId, pointLevel, custInfo.UserLevel));

            var custDepositSysIds = _bankInfoService.GetCustOffDepositSysId(custId);
            var result = offBankList
                .Where(x => !custDepositSysIds.Contains(x.SysId));

            return result;
        }
        
        public IEnumerable<OfflineBankInfoFront> GetBankInfoOffListWithCache(int custId, string lang)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var siteId = custInfo.SiteId;
            var currencyId = custInfo.CurrencyId;

            var key = $"GetBankInfoOffList:{siteId}:{currencyId}:{pointLevel}:{custInfo.UserLevel}:{lang}";
            var offBankList = _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => GetBankAccountOffList(siteId, currencyId, pointLevel, custInfo.UserLevel, lang));

            var custDepositSysIds = _bankInfoService.GetCustOffDepositSysId(custId);
            var result = offBankList
                .Select(x => new OfflineBankInfoFront(x, !custDepositSysIds.Contains(x.SysId)));
            return result;
        }

        private IEnumerable<SysBankInfoFront> GetBankAccountOffList(int siteId, int currencyId, int pointLevel, int vipLevel, string lang)
        {
            var localBankList = GetOfflineBankAccount(siteId)
                .Where(x => x.CurrencyId == currencyId 
                            && (vipLevel > pointLevel || pointLevel == 8 || x.PointLevels.Contains(pointLevel))
                            && x.VipLevels.Contains(vipLevel)
                            && x.SupportChannel.Contains(OfflineBankAccountSupportChannelEnum.LocalBank))
                .ToList();

            if (siteId == (int) SiteId.Nova88
                && currencyId == (int) CurrencyId.THB) // Nova88 泰國銀行名稱支援多語系
            {
                BankNameLocalization(localBankList, lang);
            }
            
            var depositLimitInfo = _depositService.GetDepositLimitInfoList()
                .FirstOrDefault(x => x.SiteId == siteId
                    && x.VipLevel == pointLevel // DepositLimit 裡的 VipLevel 是 PointLevel
                    && x.DepositType == (int)DepositType.LocalBank
                    && x.CurrencyId == currencyId);
            
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<SysBankInfoFront>();
            }

            if (depositLimitInfo != null && localBankList.Any())
            {
                foreach (var localBank in localBankList)
                {
                    _depositBll.FixDepositLimit(localBank, depositLimitInfo);
                }
            }

            var infos = localBankList
                .OrderBy(x => x.BSort)
                .Select( x => new SysBankInfoFront
                {
                    SysId = x.SysId,
                    BankCode = x.BankCode,
                    BankLogo = x.BankLogo,
                    BankName = x.BankName,
                    IFSCCode = x.AccountCode,
                    BankSite = x.BankSite,
                    Status = x.Status,
                    QRCodeUrl = x.QRCodeUrl,
                    SupportQR = x.SupportChannel.Any(y => y == OfflineBankAccountSupportChannelEnum.EWallet)
                })
                .Distinct(_sysBankInfoOffComparer)
                .ToList();

            foreach (var info in infos)
            {
                var tmpInfo = localBankList
                    .Where(x => x.SysId == info.SysId)
                    .Select(x => new
                    {
                        x.MinAmount,
                        x.MaxAmount
                    })
                    .OrderByDescending(x => x.MaxAmount == 0 ? decimal.MaxValue : x.MaxAmount)
                    .ThenBy(x => x.MinAmount)
                    .First();

                info.MinAmount = tmpInfo.MinAmount;
                info.MaxAmount = tmpInfo.MaxAmount;
            }
            
            return infos;
        }
        
        private IEnumerable<SysBankInfoFront> GetOfflineBankAccountList(int siteId, int currencyId, int pointLevel, int vipLevel)
        {
            var localBankList = GetOfflineBankAccount(siteId)
                .Where(x => x.CurrencyId == currencyId 
                            && (vipLevel > pointLevel || pointLevel == 8 || x.PointLevels.Contains(pointLevel)) // vipLevel > pointLevle 就先不檢查此條件，之後會改
                            && x.VipLevels.Contains(vipLevel))
                .ToList();

            var depositLimitInfo = _depositService.GetDepositLimitInfoList()
                .FirstOrDefault(x => x.SiteId == siteId
                    && x.VipLevel == pointLevel // DepositLimit 裡的 VipLevel 是 PointLevel
                    && x.DepositType == (int)DepositType.LocalBank
                    && x.CurrencyId == currencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<SysBankInfoFront>();
            }

            if (depositLimitInfo != null && localBankList.Any())
            {
                foreach (var localBank in localBankList)
                {
                    _depositBll.FixDepositLimit(localBank, depositLimitInfo);
                }
            }

            var infos = localBankList
                .OrderBy(x => x.BSort)
                .Select( x => new SysBankInfoFront
                {
                    SysId = x.SysId,
                    BankCode = x.BankCode,
                    BankLogo = x.BankLogo,
                    BankName = x.BankName,
                    IFSCCode = x.AccountCode,
                    BankSite = x.BankSite,
                    Status = x.Status,
                    QRCodeUrl = x.QRCodeUrl,
                    SupportQR = x.SupportChannel.Any(y => y == OfflineBankAccountSupportChannelEnum.EWallet),
                    MinAmount = x.MinAmount,
                    MaxAmount = x.MaxAmount
                })
                .ToList();

            return infos;
        }

        private void BankNameLocalization(in List<OfflineBankAccountInfo> list, string lang)
        {
            var bankNameList = _bankInfoBll.GetBankInfoName(lang);
            
            if (bankNameList.Any())
            {
                list.ForEach(x => x.BankName =
                    bankNameList.FirstOrDefault(y => y.SysId == x.SysId)?.BankName ?? x.BankName);
            }
        }
    }
}
