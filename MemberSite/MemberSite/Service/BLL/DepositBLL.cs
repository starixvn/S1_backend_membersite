using MemberSite.Exceptions;
using MemberSite.Extensions;
using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL;

public class DepositBLL
{
    private readonly DepositService _depositService;
    private readonly DepositLimitBLL _depositLimitBll;
    private readonly LoadCardPayInfoBLL _loadCardPayInfoBll;
    private readonly CryptoPayInfoBll _cryptoPayInfoBll;
    private readonly PayInfoBLL _payInfoBll;
    private readonly CustInfoBll _custInfoBll;

    public DepositBLL(IServiceProvider provider)
    {
        _depositService = provider.GetRequiredService<DepositService>();
        _payInfoBll = provider.GetRequiredService<PayInfoBLL>();
        _loadCardPayInfoBll = provider.GetRequiredService<LoadCardPayInfoBLL>();
        _cryptoPayInfoBll = provider.GetRequiredService<CryptoPayInfoBll>();
        _custInfoBll = provider.GetRequiredService<CustInfoBll>();
        _depositLimitBll = provider.GetRequiredService<DepositLimitBLL>();
    }

    public async Task<string> DepositReqAsync(DepositReqParams input)
    {
        if (input.Amount <= 0)
        {
            throw new CustomException($"Amount: {input.Amount} <= 0", -1);
        }
        var custId = input.CustId;


        var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
        var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

        var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, input.DepositType);
        if (depositLimitInfo?.DepositStatus == 0)
        {
            throw new CustomException(
                $"CurrencyId={custInfo.CurrencyId}, VipLevel={vipLevel}, DepositType={input.DepositType} disabled.", -2);
        }

        IPayInfo payInfo = null;
        var dic = new Dictionary<string, string>();

        switch (input.DepositType)
        {
            case DepositType.CryptoPayment:
                (payInfo, dic) = await GetCryptoPayInfo(custInfo, input.ChannelId, vipLevel, input.Amount);
                break;

            case DepositType.LoadCardPayment:
                (payInfo, dic) = GetLoadCardPayInfo(custInfo, input.ChannelId, vipLevel);
                break;

            case DepositType.MobilePayment:
            case DepositType.QrPayPayment:
            default:
                (payInfo, dic) = GenericGetPayInfo(custInfo, input.DepositType, input.ChannelId, vipLevel);
                break;
        }

        if (depositLimitInfo != null && payInfo != null)
        {
            _depositLimitBll.FixDepositLimit(payInfo, depositLimitInfo);
        }

        if (payInfo == null || payInfo.MaxAmount < input.Amount || payInfo.MinAmount > input.Amount)
        {
            throw new CustomException(
                $"Amount does not meet limit amount. PayInfo={JsonConvert.SerializeObject(payInfo)}", -3);
        }

        if (payInfo.AmountRule == 3 && !payInfo.AmountList.Contains(input.Amount))
        {
            throw new CustomException($"Amount is incorrect. AmountList={JsonConvert.SerializeObject(payInfo.AmountList)}");
        }

        foreach (var (key, value) in input.ExtraData)
        {
            dic.Add(key, value);
        }

        var depositDetailInfo = new DepositDetailInfo
        {
            Amount = input.Amount,
            CustId = custId,
            DepositType = (int)input.DepositType,
            Memo = null,
            SiteId = custInfo.SiteId,
            UserName = custInfo.UserName,
            CurrencyId = custInfo.CurrencyId,
            DepositInfo = JsonConvert.SerializeObject(dic)
        };

        _depositService.DepositReq(depositDetailInfo, out var transId, out _);

        return transId;
    }


    private static readonly IEnumerable<int> NoExchangeInfoPay
        = new List<int> {(int)PayEnum.Coin2Pay, (int)PayEnum.SDSPay};
    
    private async Task<(IPayInfo, Dictionary<string, string>)> GetCryptoPayInfo(Customer custInfo, int channelId, int vipLevel, decimal amount)
    {
        var payInfo = _payInfoBll.GetPayInfoListWithCache(custInfo.SiteId, DepositType.CryptoPayment)
            .FirstOrDefault(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId);

        if (payInfo == null)
        {
            throw new CustomException(
                $"PayInfo is null, DepositType={DepositType.CryptoPayment}, SiteId={custInfo.SiteId}, CurrencyId={custInfo.CurrencyId}, VipLevel={vipLevel}, ChannelId={channelId}");
        }

        var dic = payInfo.ClassToDic();

        if (NoExchangeInfoPay.Contains(payInfo.PayId))
        {
            var exchangeInfo =
                await _cryptoPayInfoBll.GetExchangeInfoAsync(custInfo.SiteId, custInfo.CurrencyId, amount, payInfo.ChannelName, ExchangeTypeEnum.Buy);

            if (exchangeInfo.ErrorCode != (int)ErrorCode.Success)
            {
                throw new CustomException($"{payInfo.PayName} get exchange info failed. Result={JsonConvert.SerializeObject(exchangeInfo)}");
            }

            dic.Add("ExchangeRate", exchangeInfo.Data.Rate.ToString("G29"));
            dic.Add("ExchangeAmount", exchangeInfo.Data.ExchangeAmount.ToString("G29"));
        }

        return (payInfo, dic);
    }

    private (IPayInfo, Dictionary<string, string>) GetLoadCardPayInfo(Customer custInfo, int channelId, int vipLevel)
    {
        var payInfo = _loadCardPayInfoBll.GetLoadCardPayInfoList(custInfo.SiteId)
            .FirstOrDefault(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId);

        if (payInfo == null)
        {
            throw new CustomException(
                $"PayInfo is null, DepositType={DepositType.LoadCardPayment}, SiteId={custInfo.SiteId}, CurrencyId={custInfo.CurrencyId}, VipLevel={vipLevel}, ChannelId={channelId}");
        }

        var dic = payInfo.ClassToDic();

        return (payInfo, dic);
    }

    private (IPayInfo, Dictionary<string, string>) GenericGetPayInfo(Customer custInfo, DepositType depositType, int channelId, int vipLevel)
    {
        var payInfo = _payInfoBll.GetPayInfoListWithCache(custInfo.SiteId, depositType)
            .FirstOrDefault(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.ChannelId == channelId);

        if (payInfo == null)
        {
            throw new CustomException(
                $"PayInfo is null, DepositType={depositType}, SiteId={custInfo.SiteId}, CurrencyId={custInfo.CurrencyId}, VipLevel={vipLevel}, ChannelId={channelId}");
        }

        var dic = payInfo.ClassToDic();

        return (payInfo, dic);
    }

}