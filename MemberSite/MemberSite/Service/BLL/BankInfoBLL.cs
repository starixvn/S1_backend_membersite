using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class BankInfoBLL
    {
        private readonly BankInfoService _bankInfoService;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly SysBankInfoOffComparer _sysBankInfoOffComparer;
        private readonly PaymentOffComparer _paymentOffComparer;
        private readonly PayService _payService;
        private readonly DepositBLL _depositBll;
        private readonly ICacheHelper _redisHelper;

        public BankInfoBLL(IServiceProvider provider)
        {
            _bankInfoService = provider.GetRequiredService<BankInfoService>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _payService = provider.GetRequiredService<PayService>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _sysBankInfoOffComparer = new SysBankInfoOffComparer();
            _paymentOffComparer = new PaymentOffComparer();
        }
        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrency(int currencyId)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();

            var key = $"BankInfoSysList:{serverInfo.SiteId}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromMinutes(1),
                () => _bankInfoService.GetBankInfoSysList(serverInfo.SiteId));
            return dbData?.Where(x => x.CurrencyId == currencyId);
        }

        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrencyForCtrl(int currencyId)
        {
            var serverInfo = _serverInfoBll.GetServerInfo();
            
            var key = $"BankInfoSysListForCtrl:{serverInfo.SiteId}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
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

        private class PaymentOffComparer : IEqualityComparer<PaymentFront>
        {
            public bool Equals(PaymentFront t1, PaymentFront t2)
            {
                return (t1.PayId == t2.PayId);
            }

            public int GetHashCode(PaymentFront t)
            {
                return t.ToString().GetHashCode();
            }
        }

        public IEnumerable<PaymentFront> GetOnlinePaymentList(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.FirstOrDefault(x =>
                x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel &&
                x.DepositType == (int)DepositType.OnlineBanking && x.CurrencyId == custInfo.CurrencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new PaymentFront[] { };
            }

            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId);

            var onlinePaymentList = bankInfoPayList
                .Where(x => vipLevel >= x.PointLevel && (x.CurrencyId == custInfo.CurrencyId || x.CurrencyId == 0))
                .GroupBy(x => x.PayId)
                .Select(x =>
                {
                    var d = x.OrderByDescending(g => g.CurrencyId).First();
                    return new PaymentFront
                    {
                        PayName = d.PayName,
                        PayId = d.PayId,
                        ChannelId = d.ChannelId,
                        SelBank = d.SelBank,
                        Mode = d.Mode
                    };
                }).ToList();
            
            var payCurrency = GetPayCurrency();//.Where(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == payId).FirstOrDefault()

            if (onlinePaymentList != null)
            {
                foreach (var onlinePayment in onlinePaymentList)
                {
                    var iPayCurrency = payCurrency.FirstOrDefault(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == onlinePayment.ChannelId);
                    if (iPayCurrency != null)
                    {
                        onlinePayment.MaxAmount = iPayCurrency.MaxAmount;
                        onlinePayment.MinAmount = iPayCurrency.MinAmount;

                        if (depositLimitInfo != null)
                        {
                            _depositBll.FixDepositLimit(onlinePayment, depositLimitInfo);
                        }
                    }
                }
            }

            return onlinePaymentList.Where(x => x.MaxAmount > 0);
        }

        public IEnumerable<SysBankInfoFront> GetOnlineBankInfoSysList(int custId, int payId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId);

            var bankInfoList = (from x in bankInfoPayList
                                where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.PayId == payId && (x.Method == 0 || x.Method == 1)
                                select new SysBankInfoFront
                                {
                                    SysId = x.SysId,
                                    BankCode = x.SysBankCode,
                                    BankLogo = x.BankLogo,
                                    BankName = x.SysBankName,
                                }).Distinct(_sysBankInfoOffComparer);

            return bankInfoList;
        }

        public BankInfoPayFront GetOnlineBankInfo(int custId, int sysId, int payId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.FirstOrDefault(x =>
                x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel &&
                x.DepositType == (int)DepositType.OnlineBanking && x.CurrencyId == custInfo.CurrencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return null;
            }

            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId);
            var bankInfo = (from x in bankInfoPayList
                            where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId && x.PayId == payId
                            select new BankInfoPayFront
                            {
                                SysId = x.SysId,
                                PayId = x.PayId,
                                BankCode = x.BankCode,
                                BankLogo = x.BankLogo,
                                BankName = x.SysBankName,/*x.SysBankCode*/
                                ChannelId = x.ChannelId
                            })
                .OrderByDescending(x => x.SysId)
                .FirstOrDefault();

            var payCurrency = GetPayCurrency().FirstOrDefault(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == bankInfo.ChannelId);

            if (payCurrency != null)
            {
                bankInfo.MinAmount = payCurrency.MinAmount;
                bankInfo.MaxAmount = payCurrency.MaxAmount;

                if (depositLimitInfo != null)
                {
                    _depositBll.FixDepositLimit(bankInfo, depositLimitInfo);
                }

                if (bankInfo.MaxAmount < 0)
                {
                    return null;
                }
            }

            return bankInfo;
        }
        #region China Pay

        #endregion
        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId)
        {
            var key = $"BankInfoPayList:{siteId}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<BankInfoPay>>(key, TimeSpan.FromSeconds(10),
                () => _bankInfoService.GetBankInfoPayList(siteId).OrderBy(x => x.MSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
        }
        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            var dbData = _redisHelper.TryGetOrCreate("PayCurrency", TimeSpan.FromMinutes(5),
                () => _payService.GetPayCurrency(),RedisDataBase.Shared);
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

        public IEnumerable<AutoChannelBankInfo> GetAutoChannelBankList(Customer customer, int vipLevel)
        {
            return GetAutoChannelBankInfoList(customer.SiteId, GetAutoChannelDepositType(customer.SiteId))
                    .Where(i => vipLevel >= i.PointLevel && i.CurrencyId == customer.CurrencyId && (i.Method == 0 || i.Method == 1) && i.SelBank);
        }

        public DepositType GetAutoChannelDepositType(int siteId)
        {
            //nova88使用新的DepositType，Bwing&goal123調整OnlineBanking
            return siteId == 1 ? DepositType.BankAutoChannel : DepositType.OnlineBanking;
        }

        public IEnumerable<AutoChannelBankInfo> GetAutoChannelBankInfoList(int siteId, DepositType depositType)
        {
            var key = $"BankInfoPayList:{siteId}:{depositType}";
            var dbData = _redisHelper.TryGetOrCreate(key, TimeSpan.FromSeconds(10),
                () => _bankInfoService.GetAutoChannelBankInfoList(siteId, depositType),
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
            var dbData = _redisHelper.TryGetOrCreate("EWalletInfoSysList", TimeSpan.FromSeconds(10),
                () => _bankInfoService.GetEWalletInfoSysList());
            return dbData?.Where(x => x.CurrencyId == currencyId);
        }

        public IEnumerable<BlockAccount> GetBlockEWalletAccount(int sysId, string accountNum)
        {
            return _bankInfoService.GetBlockEWalletAccount(sysId, accountNum);
        }
    }
}
