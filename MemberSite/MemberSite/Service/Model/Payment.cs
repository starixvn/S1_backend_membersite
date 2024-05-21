using MemberSite.Attributes;
using MemberSite.Service.Model.enums;

using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class PayResult
    {
        public string Url { get; set; }
        public Dictionary<string, string> FormData { get; set; }
        public int Mode { get; set; }
        public string ThirdId { get; set; }
        public string Message { get; set; }
    }

    public class LoadCardPayGroup : IPaymentLimit
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string DisplayGroupName { get; set; }
        public int GSort { get; set; }
        public string PayName { get; internal set; }
        public int Mode { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    public interface IPaymentLimit
    {
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }
    
    public interface IBankNameLocalization
    {
        public int SysId { get; set; }
        public string BankName { get; set; }
    }

    public interface IPayInfo : IPaymentLimit
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string DisplayGroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string DisplayChannelName { get; set; }
        public int Mode { get; set; }
        public int AmountRule { get; set; }
        public IEnumerable<decimal> AmountList { get; }
    }

    public class PayInfo : IPayInfo
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string DisplayGroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string DisplayChannelName { get; set; }
        public int Mode { get; set; }
        public DepositType DepositType { get; set; }
        public int ExtraDataType { get; set; }
        public int PointLevel { get; set; }
        public string PayCode { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        [DicIgnore]
        public int AmountRule { get; set; }
        [DicIgnore]
        public string AmountShow { get; set; }
        [DicIgnore]
        public IEnumerable<decimal> AmountList => AmountShow == null
            ? null : JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        [DicIgnore]
        public int GSort { get; set; }
        [DicIgnore]
        public int SSort { get; set; }
    }

    public class PayGroup : IPaymentLimit
    {

        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string DisplayGroupName { get; set; }
        public int GSort { get; set; }
        public int Mode { get; set; }
        public string ChannelName { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }


    public class PayGroupInfo
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string DisplayGroupName { get; set; }
        public DepositType Type { get; set; }
        public int Sort { get; set; }
    }

    public class PayChannelInfo
    {
        public int ChannelId { get; set; }
        public int PayId { get; set; }
        public int GroupId { get; set; }
        public string ChannelName { get; set; }
        public DepositType Type { get; set; }
        public PayChannelMethod Method { get; set; }
        public PayModeEnum Mode { get; set; }
        public ExtraDataTypeEnum ExtraDataType { get; set; }
    }

    public class LineChannel
    {
        public int Id { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; }
        public int CurrencyId { get; set; }
        public string IsoCode { get; set; }
        public string Link { get; set; }
    }

    public class ExchangeInfo
    {
        public decimal Rate { get; set; }
        public decimal To_Amount { get; set; }
        public long Updated_timestamp { get; set; }
    }

    public class CryptoExchangeInfo
    {
        public decimal Rate { get; set; }
        public decimal ExchangeAmount { get; set; }
        public long Timestamp { get; set; }
    }

    public class CryptoExchangeReq
    {
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string ChannelName { get; set; }
        public decimal Amount { get; set; }
        public ExchangeTypeEnum Type { get; set; }
    }
}
