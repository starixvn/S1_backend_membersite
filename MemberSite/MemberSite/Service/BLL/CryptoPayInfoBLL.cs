using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using System.Text.RegularExpressions;

namespace MemberSite.Service.BLL
{
    public class CryptoPayInfoBll
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositBLL _depositBll;
        private readonly PayService _payService;
        private readonly DepositService _depositService;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _payApiUrl;
        private readonly ICacheHelper _redisHelper;
        private readonly Dictionary<NetworkType, Func<string, bool>> _cryptoValidateHandler;

        public CryptoPayInfoBll(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositBll = provider.GetRequiredService<DepositBLL>();
            _payService = provider.GetRequiredService<PayService>();
            _depositService = provider.GetRequiredService<DepositService>();
            _payApiUrl = provider.GetRequiredService<IConfiguration>().GetValue<string>("PayService");
            _courierAdapter = provider.GetRequiredService<CourierAdapter>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _cryptoValidateHandler = new Dictionary<NetworkType, Func<string, bool>>
                {
                    { NetworkType.ERC20, Erc20AddressValidation },
                    { NetworkType.ETH, Erc20AddressValidation },
                    { NetworkType.TRC20, Trc20AddressValidation },
                    { NetworkType.BTC, BitCoinAddressValidation },
                    { NetworkType.BSC, Erc20AddressValidation },
                    { NetworkType.BEP20, Erc20AddressValidation },
                    { NetworkType.XRP, XRPAddressValidation },
                    { NetworkType.ADA, CardanoAddressValidation },
                };
        }

        private IEnumerable<CryptoPayInfo> GetCryptoPayInfoListWithCache(int siteId)
        {
            var key = $"PaySetting:Crypto:{siteId}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<CryptoPayInfo>>(key, TimeSpan.FromMinutes(5),
                () => _payService.GetCryptoPayInfo(siteId).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
        }
        
        public IEnumerable<CryptoPayInfo> GetCryptoPayInfoList(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfoList = _depositBll.GetDepositLimitInfoList(custInfo.SiteId);
            var depositLimitInfo = depositLimitInfoList?.Where(x => x.SiteId == custInfo.SiteId && x.VipLevel == vipLevel && x.DepositType == (int)DepositType.CryptoPayment && x.CurrencyId == custInfo.CurrencyId).FirstOrDefault();

            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new CryptoPayInfo[] { };
            }

            var cryptoPayInfoList = GetCryptoPayInfoListWithCache(custInfo.SiteId)
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId).ToList();
       

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
            
            if (cryptoPayInfo.AmountRule == 3 && !cryptoPayInfo.AmountList.Contains(amount))
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

                _depositService.DepositReq(depositDetailInfo, out transId, out _);
                return 0;
            }

            return -99;
        }

        public async Task<ApiResult<CryptoExchangeInfo>> GetExchangeInfoAsync(int siteId, int currencyId, decimal amount, string channelName, ExchangeTypeEnum type)
        { 
            var req = new CryptoExchangeReq
            {
                SiteId = siteId,
                CurrencyId = currencyId,
                Amount = amount,
                ChannelName = channelName,
                Type = type
            };
            var myUrl = $"{_payApiUrl}CryptoExchange";
            return await _courierAdapter.MyPostAsync<CryptoExchangeInfo, CryptoExchangeReq>(myUrl, req);
        }

        public bool CryptoAddressValidation(string network, string address)
        {
            var networkEnum = (NetworkType)Enum.Parse((typeof(NetworkType)), network);

            return CryptoAddressValidation(networkEnum, address);
        }

        public bool CryptoAddressValidation(NetworkType network, string address)
        {
            if (_cryptoValidateHandler.ContainsKey(network))
            {
                return _cryptoValidateHandler[network].Invoke(address);
            }

            return false;
        }

        private bool Erc20AddressValidation(string address)
        {
            var regex = new Regex(@"^0x[a-fA-F0-9]{40}$");
            return regex.IsMatch(address);
        }
        private bool Trc20AddressValidation(string address)
        {
            var regex = new Regex(@"^T[A-Za-z1-9]{33}$");
            return regex.IsMatch(address);
        }

        private bool BitCoinAddressValidation(string address)
        {
            // m/n/2 開頭為測試鏈
            var regex = new Regex(@"^(?:[1mn32]{1}[a-km-zA-HJ-NP-Z1-9]{26,33}|bc1[a-z0-9]{39,59})$");
            return regex.IsMatch(address);
        }
        
        private bool CardanoAddressValidation(string address)
        {
            var regex = new Regex(@"^(Ae2|Dd|addr|stake).*");
            return regex.IsMatch(address);
        }
        
        private bool XRPAddressValidation(string address)
        {
            var regex = new Regex(@"^r[1-9a-km-np-zA-HJ-NP-Z]{24,34}$");
            return regex.IsMatch(address);
        }
    }
}
