using System.Text.RegularExpressions;

using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class CryptoPayInfoBll
    {
        private readonly CustInfoBll _custInfoBll;
        private readonly DepositLimitBLL _depositLimitBll;
        private readonly PayService _payService;
        private readonly DepositService _depositService;
        private readonly BankInfoOffBLL _bankInfoOffBll;
        private readonly CourierAdapter _courierAdapter;
        private readonly string _payApiUrl;
        private readonly ICacheHelper _redisHelper;
        private readonly Dictionary<NetworkType, Func<string, bool>> _cryptoValidateHandler;

        public CryptoPayInfoBll(IServiceProvider provider)
        {
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositLimitBll = provider.GetRequiredService<DepositLimitBLL>();
            _bankInfoOffBll = provider.GetRequiredService<BankInfoOffBLL>();
            _payService = provider.GetRequiredService<PayService>();
            _depositService = provider.GetRequiredService<DepositService>();
            var configBll = provider.GetRequiredService<ConfigBLL>();
            _payApiUrl = configBll.GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").PayService;

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

        public IEnumerable<PayInfo> GetCryptoPayInfoListWithCache(int siteId)
        {
            var key = $"PaySetting:Crypto:{siteId}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<PayInfo>>(key, TimeSpan.FromMinutes(5),
                () => _payService.GetCryptoPayInfo(siteId).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
        }

        public IEnumerable<PayInfo> GetCryptoPayInfoList(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, DepositType.CryptoPayment);
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<PayInfo>();
            }

            var cryptoPayInfoList = GetCryptoPayInfoListWithCache(custInfo.SiteId)
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId)
                .ToList();

            var localCryptoList = _bankInfoOffBll.GetFilteredOfflineCryptoPayInfo(custId)
                .GroupBy(x => x.ChannelName)
                .Select(g =>
                    new PayInfo
                    {
                        ChannelName = g.Key,
                        Mode = 3,
                        MaxAmount = g.Any(x => x.MaxAmount == 0)
                            ? 0 : g.Max(x => x.MaxAmount),
                        MinAmount = g.Min(x => x.MinAmount),
                        ChannelId = -1 * (int)g.First().CryptoCurrencyId,
                        GroupId = -1,
                        GroupName = "LetsPay",
                        AmountRule = 1,
                        CurrencyId = custInfo.CurrencyId,
                        GSort = -1,
                        PayName = "LetsPay"
                    });

            cryptoPayInfoList.AddRange(localCryptoList);

            if (depositLimitInfo != null)
            {
                foreach (var cryptoPayInfo in cryptoPayInfoList)
                {
                    _depositLimitBll.FixDepositLimit(cryptoPayInfo, depositLimitInfo);
                }
            }

            return cryptoPayInfoList.OrderBy(x => x.GSort);
        }

        public async Task<(int ErrorCode, string Message, PayResult payResult)> DepositReqCryptoLocalPayAsync(
            Customer custInfo, decimal amount, OfflineCryptoPayInfo cryptoPayInfo)
        {
            if (amount <= 0)
                return (-1, "Amount must be greater than 0", null);

            var exchangeInfo =
                await GetExchangeInfoAsync(custInfo.SiteId, custInfo.CurrencyId, amount, cryptoPayInfo.ChannelName, ExchangeTypeEnum.Buy);

            if (exchangeInfo.ErrorCode != (int)ErrorCode.Success)
                return (-2, "Get exchange info failed.", null);

            var depositDetailInfo = new DepositDetailInfo
            {
                Amount = amount,
                CustId = custInfo.CustId,
                DepositType = (int)DepositType.CryptoWallet,
                Memo = "Local Payment",
                SiteId = custInfo.SiteId,
                UserName = custInfo.UserName,
                CurrencyId = custInfo.CurrencyId,
                DepositInfo = JsonConvert.SerializeObject(new
                {
                    cryptoPayInfo.BankId,
                    cryptoPayInfo.AccountName,
                    cryptoPayInfo.AccountNo,
                    cryptoPayInfo.FrontendDisplayName,
                    cryptoPayInfo.CryptoCurrencyId,
                    cryptoPayInfo.Network,
                    CurrencyName = cryptoPayInfo.CryptoCurrencyId.ToString(),
                    cryptoPayInfo.ChannelName,
                    ExchangeRate = exchangeInfo.Data.Rate,
                    exchangeInfo.Data.ExchangeAmount
                })
            };

            _depositService.DepositReq(depositDetailInfo, out var transId, out _);
            var payResult = new PayResult
            {
                Url = cryptoPayInfo.FrontendDisplayName,
                Mode = 3,
                FormData = new Dictionary<string, string>
                {
                    { "DepositAmount", amount.ToString() },
                    { "Rate", exchangeInfo.Data.Rate.ToString() },
                    { "RequestAmount", exchangeInfo.Data.ExchangeAmount.ToString() },
                    { "Token", cryptoPayInfo.ChannelName }
                },
                ThirdId = transId
            };

            return string.IsNullOrEmpty(transId)
                ? (-3, "Create deposit transaction failed.", null)
                : (0, transId, payResult);
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
            var regex = new Regex(@"^r[1-9a-km-zA-HJ-NP-Z]{24,34}$");
            return regex.IsMatch(address);
        }
    }
}
