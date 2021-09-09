using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MemberSite.Service.BLL
{
    public class BankInfoBLL
    {
        private readonly BankInfoService bankInfoService;
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly IMemoryCache cache;
        private readonly CustInfoBLL custInfoBLL;
        private readonly SysBankInfoOffComparer sysBankInfoOffComparer;
        private readonly PaymentOffComparer paymentOffComparer;
        private readonly PayService payService;
        private readonly DepositBLL depositBLL;

        public BankInfoBLL(IMemoryCache cache, PayService payService, BankInfoService bankInfoService, ServerInfoBLL serverInfoBLL, CustInfoBLL custInfoBLL, DepositBLL depositBLL)
        {
            this.bankInfoService = bankInfoService;
            this.serverInfoBLL = serverInfoBLL;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
            sysBankInfoOffComparer = new SysBankInfoOffComparer();
            paymentOffComparer = new PaymentOffComparer();
            this.payService = payService;
            this.depositBLL = depositBLL;
        }
        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrency(int CurrencyId)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();

            var key = $"BankInfoSysList_{serverInfo.SiteId}";
            var bankInfoSysList = cache.Get<IEnumerable<BankInfoSys>>(key);
            if (bankInfoSysList != null)
            {
                return bankInfoSysList.Where(x => x.CurrencyId == CurrencyId);
            }

            bankInfoSysList = bankInfoService.GetBankInfoSysList(serverInfo.SiteId);

            if (bankInfoSysList != null)
            {
                cache.Set(key, bankInfoSysList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
            }

            return bankInfoSysList?.Where(x => x.CurrencyId == CurrencyId);
        }

        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrencyForCtrl(int CurrencyId)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();

            var key = $"BankInfoSysListForCtrl_{serverInfo.SiteId}";
            var bankInfoSysList = cache.Get<IEnumerable<BankInfoSys>>(key);
            if (bankInfoSysList != null)
            {
                return bankInfoSysList.Where(x => x.CurrencyId == CurrencyId);
            }

            bankInfoSysList = bankInfoService.GetBankInfoSysListForCtrl(serverInfo.SiteId);

            if (bankInfoSysList != null)
            {
                cache.Set(key, bankInfoSysList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
            }

            return bankInfoSysList?.Where(x => x.CurrencyId == CurrencyId);
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
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.FirstOrDefault(x =>
                x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel &&
                x.DepositType == (int)DepositType.OnlineBanking && x.CurrencyId == custInfo.CurrencyId);

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new PaymentFront[] { };
            }

            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId);

            var onlinePaymentList = (from x in bankInfoPayList
                                     where vipLevel >= x.PointLevel && (x.CurrencyId == custInfo.CurrencyId || x.CurrencyId == 0)
                                     select new PaymentFront
                                     {
                                         PayName = x.PayName,
                                         PayId = x.PayId,
                                         ChannelId = x.ChannelId,
                                         SelBank = x.SelBank,
                                         Mode = x.Mode
                                     }).Distinct(paymentOffComparer).ToArray();
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
                            depositBLL.FixDepositLimit(onlinePayment, depositLimitInfo);
                        }
                    }
                }
            }

            return onlinePaymentList.Where(x => x.MaxAmount > 0);
        }

        public IEnumerable<SysBankInfoFront> GetOnlineBankInfoSysList(int custId, int payId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId);

            var bankInfoList = (from x in bankInfoPayList
                                where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.PayId == payId && (x.Method == 0 || x.Method == 1)
                                select new SysBankInfoFront
                                {
                                    SysId = x.SysId,
                                    BankCode = x.SysBankCode,
                                    BankLogo = x.BankLogo,
                                    BankName = x.SysBankName,
                                }).Distinct(sysBankInfoOffComparer);

            return bankInfoList;
        }

        public BankInfoPayFront GetOnlineBankInfo(int custId, int sysId, int payId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
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
                            }).FirstOrDefault();

            var payCurrency = GetPayCurrency().FirstOrDefault(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == bankInfo.ChannelId);

            if (payCurrency != null)
            {
                bankInfo.MinAmount = payCurrency.MinAmount;
                bankInfo.MaxAmount = payCurrency.MaxAmount;

                if (depositLimitInfo != null)
                {
                    depositBLL.FixDepositLimit(bankInfo, depositLimitInfo);
                }

                if (bankInfo.MaxAmount < 0)
                {
                    return null;
                }
            }

            return bankInfo;
        }
        #region China Pay
        public IEnumerable<SysBankInfoFront> GetOnlineBankInfoSysList(int custId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);
            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId);

            var bankInfoList = (from x in bankInfoPayList
                                where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId
                                select new SysBankInfoFront
                                {
                                    SysId = x.SysId,
                                    BankCode = x.SysBankCode,
                                    BankLogo = x.BankLogo,
                                    BankName = x.SysBankName,
                                }).Distinct(sysBankInfoOffComparer);

            return bankInfoList;
        }

        public BankInfoPayFront GetOnlineBankInfo(int custId, int sysId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.OnlineBanking && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return null;
            }

            var bankInfoPayList = GetOnlineBankList(custInfo.SiteId);

            var bankInfo = (from x in bankInfoPayList
                            where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId
                            orderby x.PointLevel descending, x.MSort, x.SSort
                            select new BankInfoPayFront
                            {
                                SysId = x.SysId,
                                PayId = x.PayId,
                                BankCode = x.BankCode,
                                BankLogo = x.BankLogo,
                                BankName = x.SysBankName,/*x.SysBankCode*/
                                ChannelId = x.ChannelId
                            }).FirstOrDefault();

            var payCurrency = GetPayCurrency().FirstOrDefault(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == bankInfo.ChannelId);
            if (payCurrency != null)
            {
                bankInfo.MinAmount = payCurrency.MinAmount;
                bankInfo.MaxAmount = payCurrency.MaxAmount;

                if (depositLimitInfo != null)
                {
                    depositBLL.FixDepositLimit(bankInfo, depositLimitInfo);
                }

                if (bankInfo.MaxAmount < 0)
                {
                    return null;
                }
            }

            return bankInfo;
        }
        #endregion
        public IEnumerable<BankInfoPay> GetOnlineBankList(int siteId)
        {
            var key = $"BankInfoPayList_{siteId}";
            var bankInfoPayList = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (bankInfoPayList == null)
            {
                bankInfoPayList = bankInfoService.GetBankInfoPayList(siteId).OrderBy(x => x.MSort).ThenBy(x => x.SSort);

                if (bankInfoPayList != null)
                {
                    cache.Set(key, bankInfoPayList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(3)));
                }
            }

            return bankInfoPayList;
        }
        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            var key = "PayCurrency";
            var payCurrency = cache.Get<IEnumerable<PayCurrency>>(key);

            if (payCurrency == null)
            {
                payCurrency = payService.GetPayCurrency();
                if (payCurrency != null)
                {
                    var expireTime = TimeSpan.FromMinutes(5);
                    cache.Set(key, payCurrency, new MemoryCacheEntryOptions().SetAbsoluteExpiration(expireTime));
                }
            }

            return payCurrency;
        }
    }
}
