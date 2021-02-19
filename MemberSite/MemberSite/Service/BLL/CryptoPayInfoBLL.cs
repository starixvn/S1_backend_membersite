using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class CryptoPayInfoBLL
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly DepositBLL depositBLL;
        private readonly PayService payService;
        private readonly DepoistService depoistService;
        private readonly IMemoryCache cache;
        private readonly CourierAdapter courierAdapter;
        private readonly string payApiUrl;

        public CryptoPayInfoBLL(IMemoryCache cache, IConfiguration configuration, PayService payService, DepoistService depoistService, CustInfoBLL custInfoBLL, DepositBLL depositBLL, CourierAdapter courierAdapter)
        {
            this.custInfoBLL = custInfoBLL;
            this.depositBLL = depositBLL;
            this.payService = payService;
            this.depoistService = depoistService;
            this.cache = cache;
            payApiUrl = configuration.GetValue<string>("PayService");
            this.courierAdapter = courierAdapter;
        }
        public IEnumerable<CryptoPayInfo> GetCryptoPayInfoListWithCache(int siteId)
        {
            string key = $"CryptoPayInfoList_{siteId}";
            var cryptoPayInfoList = cache.Get<IEnumerable<CryptoPayInfo>>(key);
            if (cryptoPayInfoList == null)
            {
                cryptoPayInfoList = payService.GetCryptoPayInfo(siteId).OrderBy(x => x.GSort).OrderBy(x => x.SSort);

                if (cryptoPayInfoList != null)
                {
                    cache.Set(key, cryptoPayInfoList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return cryptoPayInfoList;
        }

        public class ExchangeRate
        {
            public long Updated_timestamp { get; set; }
            public decimal BuyRate { get; set; }
            public decimal SellRate { get; set; }
        }

        public async Task<IEnumerable<CryptoPayInfo>> GetCryptoPayInfoListAsync(int custId/*, int groupId*/)
        {
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.CryptoPayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new CryptoPayInfo[] { };
            }

            var cryptoPayInfoList = GetCryptoPayInfoListWithCache(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId/* && x.GroupId == groupId*/).ToList();
            var payid = cryptoPayInfoList.FirstOrDefault()?.PayId;

            if (payid != null)
            {
                try
                {
                    var myUrl = $"{payApiUrl}ReqExchangeRate";
                    var apiResult = await courierAdapter.MyPostAsync<Dictionary<string, ExchangeRate>, dynamic>(myUrl, new { PayId = payid, CustId = custId });

                    if (apiResult.ErrorCode == 0)
                    {
                        var exchangeRate = apiResult.Data;
                        foreach (var cryptoPayInfo in cryptoPayInfoList)
                        {
                            if (exchangeRate.ContainsKey(cryptoPayInfo.GroupName))
                            {
                                cryptoPayInfo.Updated_timestamp = exchangeRate[cryptoPayInfo.GroupName].Updated_timestamp;
                                cryptoPayInfo.BuyRate = exchangeRate[cryptoPayInfo.GroupName].BuyRate;
                                cryptoPayInfo.SellRate = exchangeRate[cryptoPayInfo.GroupName].SellRate;
                            }
                        }
                    }
                }
                catch
                {

                }
            }

            if (depositLimitInfo != null && cryptoPayInfoList != null)
            {
                foreach (var cryptoPayInfo in cryptoPayInfoList)
                {
                    depositBLL.FixDepositLimit(cryptoPayInfo, depositLimitInfo);
                }
            }

            return cryptoPayInfoList.Where(x => x.MaxAmount > 0);
        }
        public int DepositReqCryptoPay(int custId, int channelId, decimal amount, out string transId, out CryptoPayInfo cryptoPayInfo)
        {
            transId = null;
            cryptoPayInfo = null;
            if (amount <= 0)
                return -1;
            var custInfo = custInfoBLL.GetCustInfoWithCache(custId);
            var vipLevel = custInfoBLL.GetPointLevelWithCache(custId);

            var depositLimitInfoList = depositBLL.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.CryptoPayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            cryptoPayInfo = GetCryptoPayInfoListWithCache(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId).FirstOrDefault();

            if (depositLimitInfo != null && cryptoPayInfo != null)
            {
                depositBLL.FixDepositLimit(cryptoPayInfo, depositLimitInfo);
            }

            if (cryptoPayInfo == null || cryptoPayInfo.MaxAmount < amount || cryptoPayInfo.MinAmount > amount)
            {
                return -2;
            }
            else if (cryptoPayInfo.AmountRule == 3 && !cryptoPayInfo.AmountList.Contains(amount))
            {
                return -2;
            }

            if (cryptoPayInfo != null)
            {
                DepositDetailInfo depositDetailInfo = new DepositDetailInfo();
                depositDetailInfo.Amount = amount;
                depositDetailInfo.CustId = custId;
                depositDetailInfo.DepositType = (int)DepositType.CryptoPayment;
                depositDetailInfo.Memo = null;
                depositDetailInfo.SiteId = custInfo.SiteId;
                depositDetailInfo.UserName = custInfo.UserName;
                depositDetailInfo.CurrencyId = custInfo.CurrencyId;
                depositDetailInfo.DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(cryptoPayInfo);
                string postScript = null;

                depoistService.DepositReq(depositDetailInfo, out transId, out postScript);
                return 0;
            }

            return -99;
        }

    }
}
