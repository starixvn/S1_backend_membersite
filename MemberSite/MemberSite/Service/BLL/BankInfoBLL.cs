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

        public BankInfoBLL(IServiceProvider provider)
        {
            _bankInfoService = provider.GetRequiredService<BankInfoService>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _payService = provider.GetRequiredService<PayService>();
            _depositLimitBll = provider.GetRequiredService<DepositLimitBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
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

        public void BankNameLocalization(in List<BankInfoSys> list, string lang)
        {
            var bankNameList = GetBankInfoName(lang);
            if (bankNameList.Any())
            {
                list.ForEach(x => x.BankName =
                    bankNameList.FirstOrDefault(y => y.SysId == x.SysId)?.BankName ?? x.BankName);
            }
        }

        public void BankNameLocalization(in List<SysBankInfoFront> list, string lang)
        {
            var bankNameList = GetBankInfoName(lang);
            if (bankNameList.Any())
            {
                list.ForEach(x => x.BankName =
                    bankNameList.FirstOrDefault(y => y.SysId == x.SysId)?.BankName ?? x.BankName);
            }
        }

        public IEnumerable<PaymentFront> GetOnlinePaymentList(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var pointLevel = _custInfoBll.GetPointLevelWithCache(custId);
            
            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.OnlineBanking);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<PaymentFront>();
            }

            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId)
                .Where(x => pointLevel >= x.PointLevel
                    && (x.CurrencyId == custInfo.CurrencyId || x.CurrencyId == 0));

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
                        SelBank = item.SelBank,
                        Mode = item.Mode,
                        MaxAmount = item.MaxAmount,
                        MinAmount = item.MinAmount
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
            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId);
            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.OnlineBanking);

            var bankInfoList = bankInfoPayList
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.PayId == payId &&
                            x.Method is 0 or 1)
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

            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId)
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
                .OrderByDescending(x => x.SysId)
                .FirstOrDefault();

            return bankInfo;
        }
        #region China Pay

        #endregion
        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId)
        {
            var key = $"PaySetting:OnlineBanking:{siteId}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<BankInfoPay>>(key, TimeSpan.FromMinutes(5),
                () => _bankInfoService.GetBankInfoPayList(siteId, DepositType.OnlineBanking).OrderBy(x => x.MSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
        }
        public IEnumerable<SysBankInfoFront> GetAutoChannelBankGroupList(Customer customer, int vipLevel)
        {
            return GetAutoChannelBankList(customer, vipLevel)
                    .GroupBy(bank => bank.SysId)
                    .Select(reuslt => new SysBankInfoFront
                    {
                        SysId = reuslt.Key,
                        BankCode = reuslt.First().SysBankCode,
                        BankLogo = reuslt.First().BankLogo,
                        BankName = reuslt.First().SysBankName,
                        MaxAmount = reuslt.Max(i => i.MaxAmount),
                        MinAmount = reuslt.Min(i => i.MinAmount),
                    });
        }

        public IEnumerable<BankInfoPay> GetAutoChannelBankList(Customer customer, int vipLevel)
        {
            return GetAutoChannelBankInfoList(customer.SiteId, GetAutoChannelDepositType(customer.SiteId))
                    .Where(i => vipLevel >= i.PointLevel && i.CurrencyId == customer.CurrencyId && (i.Method == 0 || i.Method == 1) && i.SelBank);
        }

        public DepositType GetAutoChannelDepositType(int siteId)
        {
            //nova88使用新的DepositType，Bwing&goal123調整OnlineBanking
            return siteId == 1 ? DepositType.BankAutoChannel : DepositType.OnlineBanking;
        }

        public IEnumerable<BankInfoPay> GetAutoChannelBankInfoList(int siteId, DepositType depositType)
        {
            var key = $"PaySetting:OnlineBanking:{siteId}:{depositType}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(5),
                () => _bankInfoService.GetBankInfoPayList(siteId, depositType),
                RedisDataBase.Shared);
            return dbData;
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
