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
        private readonly PaymentBLL paymentBLL;

        public BankInfoBLL(IMemoryCache cache, PaymentBLL paymentBLL, BankInfoService bankInfoService, ServerInfoBLL serverInfoBLL, CustInfoBLL custInfoBLL)
        {
            this.bankInfoService = bankInfoService;
            this.serverInfoBLL = serverInfoBLL;
            this.cache = cache;
            this.custInfoBLL = custInfoBLL;
            sysBankInfoOffComparer = new SysBankInfoOffComparer();
            this.paymentBLL = paymentBLL;
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
        public IEnumerable<SysBankInfoFront> GetOnlineBankInfoSysList(int custId)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankInfoPayList = bankInfoService.GetBankInfoPayList();

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
            var vipLevel = custInfoBLL.GetVipLevelWithCache(custId);
            var bankInfoPayList = bankInfoService.GetBankInfoPayList();

            var bankInfo = (from x in bankInfoPayList
                            where vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.SysId == sysId
                            orderby x.PointLevel descending, x.MSort, x.SSort
                            select new BankInfoPayFront
                            {
                                SysId = x.SysId,
                                BankCode = x.BankCode,
                                BankLogo = x.BankLogo,
                                BankName = x.SysBankName,/*x.SysBankCode*/
                                ChannelId = x.ChannelId
                            }).FirstOrDefault();

            var payCurrency = paymentBLL.GetPayCurrency().Where(x => x.CurrencyId == custInfo.CurrencyId && x.ChannelId == bankInfo.ChannelId).FirstOrDefault();
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
            if (bankInfoPayList != null)
            {
                return bankInfoPayList;
            }

            bankInfoPayList = bankInfoService.GetBankInfoPayList();

            if (bankInfoPayList != null)
            {
                cache.Set(key, bankInfoPayList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));
            }

            return bankInfoPayList;
        }
    }
}
