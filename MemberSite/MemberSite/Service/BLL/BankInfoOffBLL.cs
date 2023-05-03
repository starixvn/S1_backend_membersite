using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Library;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class BankInfoOffBLL
    {
        private readonly BankInfoOffService _bankInfoService;
        private readonly BankInfoBLL _bankInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositBLL _depositBll;
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
            _depositService = provider.GetRequiredService<DepositService>();
            _logger = provider.GetRequiredService<ILogger<BankInfoOffBLL>>();
        }

        public IEnumerable<OfflineBankAccountInfo> GetOfflineBankAccount(int siteId)
        {
            return _bankInfoService.GetOfflineBankAccount(siteId)
                .Where(x => x.Purposes.Contains(OfflineBankAccountPurposeEnum.Deposit));
        }
        
        public IEnumerable<SysBankInfoFront> GetOfflineBankCardListWithCache(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var siteId = custInfo.SiteId;
            var currencyId = custInfo.CurrencyId;

            var key = $"PaySetting:LocalBank:FakeMobilePay:{siteId}:{currencyId}:{pointLevel}:{custInfo.UserLevel}";
            var offBankList = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
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

            var key = $"PaySetting:LocalBank:{siteId}:{currencyId}:{pointLevel}:{custInfo.UserLevel}:{lang}";
            var offBankList = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => GetBankAccountOffList(siteId, currencyId, pointLevel, custInfo.UserLevel, lang));

            var custDepositSysIds = _bankInfoService.GetCustOffDepositSysId(custId);
            var result = offBankList
                .Where(x => !x.SupportChannel.Any() 
                            || x.SupportChannel.Contains(OfflineBankAccountSupportChannelEnum.LocalBank))
                .Select(x => new OfflineBankInfoFront(x, !custDepositSysIds.Contains(x.SysId)));
            return result;
        }
        
        public int LocalBankinfo(int custId, int sysId, string memo, out DepositRep3 depositRep3)
        {
            depositRep3 = new DepositRep3();

            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var localBankList = GetOfflineBankAccount(custInfo.SiteId)
                .Where(x => x.CurrencyId == custInfo.CurrencyId
                            && (custInfo.UserLevel > pointLevel || pointLevel == 8 || x.PointLevels.Contains(pointLevel))
                            && x.VipLevels.Contains(custInfo.UserLevel)
                            && x.SysId == sysId)
                .ToList();
            var depositLimitInfoList = _depositService.GetDepositLimitInfoList();
            var depositLimitInfo = depositLimitInfoList?
                .Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == pointLevel && x.DepositType == (int)DepositType.LocalBank && x.CurrencyId == custInfo.CurrencyId)
                .FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -4;
            }

            if (depositLimitInfo != null && localBankList != null)
            {
                foreach (var localBack in localBankList)
                {
                    _depositBll.FixDepositLimit(localBack, depositLimitInfo);
                }
            }
            
            // 排除超過每日存款限額的卡
            var bankIds = localBankList.Where(x => x.DailyLimitAmount > 0).Select(x => x.BankId);
            if (bankIds.Any())
            {
                var d = GetOfflineBankAccountDailyDepositWithCache(custInfo.SiteId, custInfo.CurrencyId, bankIds);
                localBankList = localBankList.Where(x =>
                        !d.Any(y => y.BankId == x.BankId && y.DailyDeposit >= x.DailyLimitAmount))
                    .ToList();
            }

            var info = localBankList
                .Where(x => !x.SupportChannel.Any() 
                    || x.SupportChannel.Contains(OfflineBankAccountSupportChannelEnum.LocalBank))
                .OrderBy(x => x.BSort)
                .Select(x => new
                {
                    x.SysId,
                    x.BankId,
                    x.BankLogo,
                    x.BankName,
                    x.BankSite,
                    x.AccountName,
                    x.AccountNo,
                    IFSCCode = x.AccountCode
                }).FirstOrDefault();

            if (info != null)
            {
                var depositDetailInfo = new DepositDetailInfo
                {
                    CustId = custId,
                    DepositType = (int)DepositType.LocalBank,
                    Memo = memo,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId
                };

                var depositInfo3_1 = new DepositInfo3_1
                {
                    AccountName = info.AccountName,
                    CurrencyId = custInfo.CurrencyId,
                    SysId = info.SysId,
                    AccountNo = info.AccountNo,
                    IFSCCode = info.IFSCCode,
                    Ver = 1
                };
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(depositInfo3_1);

                var transId = "";
                var postScript = "";
                depositRep3.TransId = transId;
                depositRep3.PostScript = postScript;
                depositRep3.AccountNo = depositInfo3_1.AccountNo;
                depositRep3.AccountName = depositInfo3_1.AccountName;
                depositRep3.IFSCCode = depositInfo3_1.IFSCCode;
                depositRep3.BankName = info.BankName;
                depositRep3.BankLogo = info.BankLogo;
                depositRep3.BankSite = info.BankSite;

                return 0;
            }

            var tmpInfo = localBankList.
                Select(x => new
                {
                    x.SysId,
                    x.BankId,
                    x.BankLogo,
                    x.BankName,
                    x.BankSite,
                    x.BSort,
                    x.AccountName,
                    x.AccountNo,
                    IFSCCode = x.AccountCode,
                    x.MinAmount,
                    x.MaxAmount
                })
                .OrderByDescending(x => x.MaxAmount)
                .ThenBy(x => x.MinAmount)
                .ThenBy(x => x.BSort)
                .FirstOrDefault();
            
            if (info == null && tmpInfo == null)
            {
                return -2;
            }

            if (info == null && tmpInfo != null)
            {
                depositRep3.MaxAmount = tmpInfo.MaxAmount;
                depositRep3.MinAmount = tmpInfo.MinAmount;

                return -3;
            }

            return 0;
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

            // 排除超過每日存款限額的卡
            if (localBankList.Any(x => x.DailyLimitAmount > 0))
            {
                var bankIds = localBankList.Where(x => x.DailyLimitAmount > 0).Select(x => x.BankId);
                var d = GetOfflineBankAccountDailyDepositWithCache(siteId, currencyId, bankIds);
                localBankList = localBankList.Where(x =>
                    !d.Any(y => y.BankId == x.BankId && y.DailyDeposit >= x.DailyLimitAmount))
                    .ToList();
            }

            var infos = localBankList
                .GroupBy(x => new {x.SysId, x.BankCode})
                .Select(x =>
                {
                    var d = x.OrderBy(y => y.BSort).First();

                    return new SysBankInfoFront
                    {
                        SysId = d.SysId,
                        BankCode = d.BankCode,
                        BankLogo = d.BankLogo,
                        BankName = d.BankName,
                        IFSCCode = d.AccountCode,
                        BankSite = d.BankSite,
                        Status = d.Status,
                        QRCodeUrl = d.QRCodeUrl,
                        SupportChannel = d.SupportChannel,
                        MinAmount = x.Min(y => y.MinAmount),
                        MaxAmount = x.Max(y => y.MaxAmount)
                    };
                });

            
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
                    BankId = x.BankId,
                    SysId = x.SysId,
                    BankCode = x.BankCode,
                    BankLogo = x.BankLogo,
                    BankName = x.BankName,
                    IFSCCode = x.AccountCode,
                    BankSite = x.BankSite,
                    Status = x.Status,
                    QRCodeUrl = x.QRCodeUrl,
                    SupportChannel = x.SupportChannel,
                    MinAmount = x.MinAmount,
                    MaxAmount = x.MaxAmount,
                    GroupIds = x.GroupIds
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
        
        public IEnumerable<OfflineBankAccountDailyDeposit> GetOfflineBankAccountDailyDepositWithCache(
            int siteId, int currencyId, IEnumerable<int> bankIds)
        {
            var strBankIds = JsonConvert.SerializeObject(bankIds);
            var key = $"PaySetting:LocalBank:DailyDeposit:{siteId}:{currencyId}:{strBankIds}";
            
            return _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => _bankInfoService.GetOfflineBankAccountDailyDeposit(siteId, currencyId, strBankIds));
        }

        public IEnumerable<LocalPaySetting> GetLocalPaymentSettingWithCache()
        {
            return _redisHelper.TryGetOrCreate("PaySetting:LocalPaymentSetting", TimeSpan.FromMinutes(5),
                () => _bankInfoService.GetLocalPaySetting());
        }
    }
}
