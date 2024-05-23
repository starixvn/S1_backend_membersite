using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class BankInfoBLL
    {
        private readonly BankInfoService _bankInfoService;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly PayService _payService;
        private readonly DepositLimitBLL _depositLimitBll;
        private readonly ICacheHelper _redisHelper;
        private readonly PayInfoLocalizeBLL _payInfoLocalizeBll;

        public BankInfoBLL(IServiceProvider provider)
        {
            _bankInfoService = provider.GetRequiredService<BankInfoService>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _payService = provider.GetRequiredService<PayService>();
            _depositLimitBll = provider.GetRequiredService<DepositLimitBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _payInfoLocalizeBll = provider.GetRequiredService<PayInfoLocalizeBLL>();
        }
        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrency(int currencyId)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();

            var key = $"BankInfoSysList:{serverInfo.SiteId}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => _bankInfoService.GetBankInfoSysList(serverInfo.SiteId));
            return dbData?.Where(x => x.CurrencyId == currencyId);
        }

        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrencyForCtrl(int currencyId)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();

            var key = $"BankInfoSysListForCtrl:{serverInfo.SiteId}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(1), // BankInfoSys後台不能開關
                () => _bankInfoService.GetBankInfoSysListForCtrl(serverInfo.SiteId));
            return dbData?.Where(x => x.CurrencyId == currencyId);
        }

        public IEnumerable<BankInfoName> GetBankInfoName(string lang)
        {
            var key = $"BankInfoNameList:{lang}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => _bankInfoService.GetBankInfoNameList(lang));
            return dbData;
        }

        public void BankNameLocalization<T>(in IEnumerable<T> list, string lang) where T : class, IBankNameLocalization
        {
            var bankNameList = GetBankInfoName(lang);

            if (!bankNameList.Any())
                return;
            
            foreach (var item in list)
            {
                item.BankName = bankNameList.FirstOrDefault(y => y.SysId == item.SysId)
                    ?.BankName ?? item.BankName;
            }
        }

        public IEnumerable<PaymentFront> GetOnlinePaymentList(int custId, DepositType depositType)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, depositType);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<PaymentFront>();
            }

            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId, depositType)
                .Where(x => pointLevel >= x.PointLevel
                    && (x.CurrencyId == custInfo.CurrencyId || x.CurrencyId == 0));

            var channelNames = _payInfoLocalizeBll.GetPayChannelNameWithCache();

            var onlinePaymentList = bankInfoPayList
                .GroupBy(x => x.ChannelId)
                .Select(x =>
                {
                    var item = x.First();
                    var p = new PaymentFront
                    {
                        PayName = item.PayName,
                        PayId = item.PayId,
                        ChannelId = item.ChannelId,
                        DisplayChannelName = channelNames
                            .FirstOrDefault(y => y.ChannelId == item.ChannelId)?.ChannelName ?? item.PayName,
                        SelBank = item.SelBank,
                        Mode = item.Mode,
                        ExtraDataType = item.ExtraDataType,
                        MaxAmount = item.MaxAmount,
                        MinAmount = item.MinAmount,
                        BankList = item.SelBank ?
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
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId, DepositType.OnlineBanking);
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
                        BankName = d.SysBankName,
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

            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId, DepositType.OnlineBanking)
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
            var key = $"PaySetting:OnlineBanking:{siteId}:{depositType}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<BankInfoPay>>(key, TimeSpan.FromMinutes(5),
                () => _bankInfoService.GetBankInfoPayList(siteId, depositType).OrderBy(x => x.MSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
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
            return GetOnlineBankList(customer.SiteId, type).Where(i =>
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
