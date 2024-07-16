using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class BankInfoBLL
    {
        private readonly BankInfoService _bankInfoService;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly PayService _payService;
        private readonly DepositLimitBLL _depositLimitBll;
        private readonly ICacheHelper _redisHelper;

        public BankInfoBLL(IServiceProvider provider)
        {
            _bankInfoService = provider.GetRequiredService<BankInfoService>();
            _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _payService = provider.GetRequiredService<PayService>();
            _depositLimitBll = provider.GetRequiredService<DepositLimitBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }
        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrency(int currencyId, string lang)
        {
            var key = $"PaySetting:BankInfoSys:{currencyId}:{lang}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(10),
                () => _bankInfoService.GetBankInfoSysList(currencyId, lang), RedisDataBase.Shared);
            
            return dbData;
        }

        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrencyForCtrl(int currencyId, string lang = "")
            => GetBankInfoSysListByCurrency(currencyId, lang)
                .Where(x => x.Status == GeneralStatusEnum.Enable);
        

        public IEnumerable<PaymentFront> GetOnlinePaymentList(int custId, DepositType depositType)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, depositType);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<PaymentFront>();
            }

            var bankInfoPayList = GetOnlineBankListLocalizationWithCache(custInfo, depositType)
                .Where(x => pointLevel >= x.PointLevel
                    && (x.CurrencyId == custInfo.CurrencyId || x.CurrencyId == 0));

            var onlinePaymentList = bankInfoPayList
                .GroupBy(x => x.ChannelId)
                .Select(x =>
                {
                    var item = x.First();
                    var selBank = item.SelBank && item.ExtraDataType == ExtraDataTypeEnum.None;
                    var p = new PaymentFront
                    {
                        PayName = item.PayName,
                        PayId = item.PayId,
                        ChannelId = item.ChannelId,
                        SelBank = selBank,
                        Mode = item.Mode,
                        ExtraDataType = (int)item.ExtraDataType,
                        MaxAmount = item.MaxAmount,
                        MinAmount = item.MinAmount,
                        BankList = selBank ?
                            x.Select(y => new BankInfoSys
                            {
                                BankLogo = y.BankLogo,
                                BankName = y.BankName,
                                SysId = y.SysId,
                                BankCode = y.BankCode
                            }) : Array.Empty<BankInfoSys>()
                    };

                    if (depositLimitInfo != null)
                    {
                        _depositLimitBll.FixDepositLimit(p, depositLimitInfo);
                    }

                    return p;
                });

            return onlinePaymentList.Where(x => x.MaxAmount > 0);
        }

        public IEnumerable<SysBankInfoFront> GetOnlineBankInfoSysList(int custId, int payId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var bankInfoPayList = GetOnlineBankListLocalizationWithCache(custInfo, DepositType.OnlineBanking);
            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.OnlineBanking);

            var bankInfoList = bankInfoPayList
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.PayId == payId)
                .GroupBy(x => new { x.SysId, x.BankCode })
                .Select(x =>
                {
                    var d = x.First();
                    var p = new SysBankInfoFront
                    {
                        SysId = d.SysId,
                        BankCode = d.SysBankCode,
                        BankLogo = d.BankLogo,
                        BankName = d.BankName,
                        MaxAmount = d.MaxAmount,
                        MinAmount = d.MinAmount
                    };

                    if (depositLimitInfo != null)
                    {
                        _depositLimitBll.FixDepositLimit(p, depositLimitInfo);
                    }
                    return p;
                });

            return bankInfoList;
        }

        public BankInfoPayFront GetOnlineBankInfo(int custId, int sysId, int payId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.OnlineBanking);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return null;
            }

            var bankInfoPayList = GetOnlineBankListLocalizationWithCache(custInfo, DepositType.OnlineBanking)
                .Where(x => pointLevel >= x.PointLevel
                            && x.CurrencyId == custInfo.CurrencyId
                            && x.SysId == sysId
                            && x.PayId == payId);

            var bankInfo = bankInfoPayList
                .Select(x =>
                {
                    var p = new BankInfoPayFront
                    {
                        SysId = x.SysId,
                        PayId = x.PayId,
                        BankCode = x.SysBankCode,
                        BankLogo = x.BankLogo,
                        BankName = x.SysBankName,
                        ChannelId = x.ChannelId,
                        MaxAmount = x.MaxAmount,
                        MinAmount = x.MinAmount
                    };

                    if (depositLimitInfo != null)
                    {
                        _depositLimitBll.FixDepositLimit(p, depositLimitInfo);
                    }

                    return p;
                })
                .Where(x => x.MaxAmount >= 0)
                .MaxBy(x => x.SysId);

            return bankInfo;
        }
        
        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId, DepositType depositType)
        {
            var key = $"PaySetting:{siteId}:{DepositType.OnlineBanking}:{depositType}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<BankInfoPay>>(key, TimeSpan.FromSeconds(30),
                () => _bankInfoService.GetBankInfoPayList(siteId, depositType).OrderBy(x => x.MSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
        }
        
        private IEnumerable<BankInfoPay> GetOnlineBankListLocalizationWithCache(Customer custInfo, DepositType depositType)
        {
            var authData = _authorizeBll.GetAuthData();
            var key = $"PaySetting:{custInfo.SiteId}:{DepositType.OnlineBanking}:{custInfo.CurrencyId}:{authData.LangId}:{depositType}";
            
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<BankInfoPay>>(key, TimeSpan.FromSeconds(30),
                () =>GetOnlineBankListLocalization(custInfo, authData.LangId, depositType).OrderBy(x => x.MSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            
            return dbData;
        }
        
        private IEnumerable<BankInfoPay> GetOnlineBankListLocalization(Customer custInfo, string lang, DepositType depositType)
        {
            var sysBankList = GetBankInfoSysListByCurrency(custInfo.CurrencyId, lang);
            var onlineBankList = GetOnlineBankList(custInfo.SiteId, depositType)
                .Where(x => x.CurrencyId == custInfo.CurrencyId)
                .Select(x =>
                {
                    var sys = sysBankList.FirstOrDefault(s => s.SysId == x.SysId);

                    if (sys != null)
                        x.BankName = sys.BankName;

                    return x;
                });

            return onlineBankList;
        }
        
        public IEnumerable<SysBankInfoFront> GetAutoSelectChannelBankGroupList(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            
            return GetSelectBankBankList(custInfo, vipLevel, DepositType.OnlineBanking)
                    .GroupBy(bank => bank.SysId)
                    .Select(r => new SysBankInfoFront
                    {
                        SysId = r.Key,
                        BankCode = r.First().SysBankCode,
                        BankLogo = r.First().BankLogo,
                        BankName = r.First().SysBankName,
                        MaxAmount = r.Max(i => i.MaxAmount),
                        MinAmount = r.Min(i => i.MinAmount),
                    });
        }

        public IEnumerable<BankInfoPay> GetSelectBankBankList(Customer customer, int vipLevel, DepositType type)
        {
            return GetOnlineBankListLocalizationWithCache(customer, type).Where(i =>
                vipLevel >= i.PointLevel
                && i.CurrencyId == customer.CurrencyId
                && i.SelBank);
        }

        public LineChannel? GetLineChannel(int siteId, int currencyId)
        {
            return _payService.GetLineChannel(siteId, currencyId);
        }

        public IEnumerable<BlockAccount> GetBlockBankAccount(int sysId, string accountNum)
        {
            return _bankInfoService.GetBlockBankAccount(sysId, accountNum);
        }

        public IEnumerable<EWalletInfoSys> GetEWalletInfoSysListByCurrency(int currencyId)
        {
            var dbData = _redisHelper.TryGetOrCreate("EWalletInfoSysList", TimeSpan.FromMinutes(5),
                () => _bankInfoService.GetEWalletInfoSysList());
            return dbData?.Where(x => x.CurrencyId == currencyId);
        }

        public IEnumerable<BlockAccount> GetBlockEWalletAccount(int sysId, string accountNum)
        {
            return _bankInfoService.GetBlockEWalletAccount(sysId, accountNum);
        }

        public IEnumerable<BlockCryptoWallet> GetBlockCryptoWallet(string address)
        {
            return _bankInfoService.GetBlockCryptoWallet(address);
        }
    }
}
