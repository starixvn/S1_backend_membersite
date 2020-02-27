using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public BankInfoBLL(IMemoryCache cache, PayService payService,  BankInfoService bankInfoService, ServerInfoBLL serverInfoBLL, CustInfoBLL custInfoBLL)
        {
            this.bankInfoService = bankInfoService;
            this.serverInfoBLL = serverInfoBLL;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
            sysBankInfoOffComparer = new SysBankInfoOffComparer();
            paymentOffComparer = new PaymentOffComparer();
            this.payService = payService;
        }
        public IEnumerable<BankInfoSys> GetBankInfoSysListByCurrency(int CurrencyId)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();

            string key = $"BankInfoSysList_{serverInfo.SiteId}";
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

            string key = $"BankInfoSysListForCtrl_{serverInfo.SiteId}";
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
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankInfoPayList = GetOnlineBankList();

            var onlinePaymentList = (from x in bankInfoPayList
                                     where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId
                                     select new PaymentFront
                                     {
                                         PayName = x.PayName,
                                         PayId = x.PayId,
                                         ChannelId = x.ChannelId
                                     }).Distinct(paymentOffComparer).ToArray();
            var payCurrency = GetPayCurrency();//.Where(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == payId).FirstOrDefault()
            if (onlinePaymentList != null)
            {
                foreach(var onlinePayment in onlinePaymentList)
                {
                    var iPayCurrency = payCurrency.Where(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == onlinePayment.ChannelId).FirstOrDefault();
                    if (iPayCurrency != null)
                    {
                        onlinePayment.MaxAmount = iPayCurrency.MaxAmount;
                        onlinePayment.MinAmount = iPayCurrency.MinAmount;
                    }
                }
            }

            return onlinePaymentList;
        }

        public IEnumerable<SysBankInfoFront> GetOnlineBankInfoSysList(int custId, int payId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankInfoPayList = GetOnlineBankList();

            var bankInfoList = (from x in bankInfoPayList
                                where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.PayId == payId
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
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankInfoPayList = GetOnlineBankList();

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

            var payCurrency = GetPayCurrency().Where(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == bankInfo.ChannelId).FirstOrDefault();
            if (payCurrency != null)
            {
                bankInfo.MinAmount = payCurrency.MinAmount;
                bankInfo.MaxAmount = payCurrency.MaxAmount;
            }

            return bankInfo;
        }

        public IEnumerable<BankInfoPay> GetOnlineBankList()
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            string key = $"BankInfoPayList_{serverInfo.SiteId}";
            var bankInfoPayList = cache.Get<IEnumerable<BankInfoPay>>(key);
            if (bankInfoPayList == null)
            {
                bankInfoPayList = bankInfoService.GetBankInfoPayList();

                if (bankInfoPayList != null)
                {
                    cache.Set(key, bankInfoPayList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));
                }
            }

            return bankInfoPayList;
        }
        public IEnumerable<PayCurrency> GetPayCurrency()
        {
            string key = "PayCurrency";
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
