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

        public BankInfoBLL(IMemoryCache cache, BankInfoService bankInfoService, ServerInfoBLL serverInfoBLL)
        {
            this.bankInfoService = bankInfoService;
            this.serverInfoBLL = serverInfoBLL;
            this.cache = cache;
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

        public IEnumerable<BankInfoPay> GetOnlineBankInfoSysList()
        {
            return bankInfoService.GetBankInfoPayList();
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
