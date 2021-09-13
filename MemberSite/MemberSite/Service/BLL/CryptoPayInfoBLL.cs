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
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.BLL
{
    public class CryptoPayInfoBll
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositBLL _depositBll;
        private readonly PayService _payService;
        private readonly DepoistService _depositService;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _payApiUrl;
        private readonly CacheHelper _cacheHelper;

        public CryptoPayInfoBll(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _payService = provider.GetRequiredService<PayService>();
            _depositService = provider.GetRequiredService<DepoistService>();
            _payApiUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("PayService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }

        private IEnumerable<CryptoPayInfo> GetCryptoPayInfoListWithCache(int siteId)
        {
            var key = $"CryptoPayInfoList_{siteId}";
            return _cacheHelper.CacheTryGetValueSet<IEnumerable<CryptoPayInfo>>(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1)),
                i => _payService.GetCryptoPayInfo(siteId).OrderBy(x => x.GSort).ThenBy(x => x.SSort));
        }
        
        public IEnumerable<CryptoPayInfo> GetCryptoPayInfoList(int custId/*, int groupId*/)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.CryptoPayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new CryptoPayInfo[] { };
            }

            var cryptoPayInfoList = GetCryptoPayInfoListWithCache(custInfo.SiteId).Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId/* && x.GroupId == groupId*/).ToList();
            var payid = cryptoPayInfoList.FirstOrDefault()?.PayId;

            //if (payid != null)
            //{
            //    try
            //    {
            //        var myUrl = $"{payApiUrl}ReqExchangeRate";
            //        var apiResult = await courierAdapter.MyPostAsync<Dictionary<string, ExchangeRate>, dynamic>(myUrl, new { PayId = payid, CustId = custId });

            //        if (apiResult.ErrorCode == 0)
            //        {
            //            var exchangeRate = apiResult.Data;
            //            foreach (var cryptoPayInfo in cryptoPayInfoList)
            //            {
            //                if (exchangeRate.ContainsKey(cryptoPayInfo.GroupName))
            //                {
            //                    cryptoPayInfo.Updated_timestamp = exchangeRate[cryptoPayInfo.GroupName].Updated_timestamp;
            //                    cryptoPayInfo.BuyRate = exchangeRate[cryptoPayInfo.GroupName].BuyRate;
            //                    cryptoPayInfo.SellRate = exchangeRate[cryptoPayInfo.GroupName].SellRate;
            //                }
            //            }
            //        }
            //    }
            //    catch
            //    {

            //    }
            //}

            if (depositLimitInfo != null)
            {
                foreach (var cryptoPayInfo in cryptoPayInfoList)
                {
                    _depositBll.FixDepositLimit(cryptoPayInfo, depositLimitInfo);
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
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.CryptoPayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return -3;
            }

            cryptoPayInfo = GetCryptoPayInfoListWithCache(custInfo.SiteId).FirstOrDefault(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId);

            if (depositLimitInfo != null && cryptoPayInfo != null)
            {
                _depositBll.FixDepositLimit(cryptoPayInfo, depositLimitInfo);
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
                var depositDetailInfo = new DepositDetailInfo
                {
                    Amount = amount,
                    CustId = custId,
                    DepositType = (int)DepositType.CryptoPayment,
                    Memo = null,
                    SiteId = custInfo.SiteId,
                    UserName = custInfo.UserName,
                    CurrencyId = custInfo.CurrencyId,
                    DepositInfo = Newtonsoft.Json.JsonConvert.SerializeObject(cryptoPayInfo)
                };
                string postScript = null;

                _depositService.DepositReq(depositDetailInfo, out transId, out postScript);
                return 0;
            }

            return -99;
        }

        public async Task<ApiResult<PaymentReq>> GetExchangeInfoAsync(int custId, decimal amount,int payId)
        { 
            var exchangeReq = new 
            {
                CustId = custId,
                Amount = amount,
                PayId = payId

            };
            var myUrl = $"{_payApiUrl}CryptoExchange";
            return await _courierAdapter.MyPostAsync<PaymentReq, dynamic>(myUrl, exchangeReq);
        }
    }
    
}
