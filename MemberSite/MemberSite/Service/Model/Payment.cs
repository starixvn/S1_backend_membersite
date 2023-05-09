using MemberSite.Extensions;
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

    public class PayCurrency : IPaymentLimit
    {
        public int ChannelId { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        public string AmountShow { get; set; }
    }
    public class MobilePayInfo : IPaymentLimit
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public int GSort { get; set; }
        public int SSort { get; set; }
    }

    public class MobilePayGroup
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int GSort { get; set; }
        public int Mode { get; set; }
        public string ChannelName { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
    }

    public class CryptoPayInfo : IPaymentLimit
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string CurrencyName => ChannelName?.Split('-').FirstOrDefault();
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public string PayCode { get; set; }
        public int GSort { get; set; }
        public int SSort { get; set; }
    }
    public class CryptoPayGroup
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int GSort { get; set; }
    }

    public class LoadCardPayInfo : IPaymentLimit
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public string PayCode { get; set; }
        public int GSort { get; set; }
        public int SSort { get; set; }
        public long Updated_timestamp { get; set; }
        public string Code { get; set; }
        public string Serial { get; set; }
    }
    public class LoadCardPayGroup : IPaymentLimit
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
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

    public class PayInfo : IPaymentLimit
    {
        public int PayId { get; set; }
        public string PayName { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int ChannelId { get; set; }
        public string ChannelName { get; set; }
        public int Mode { get; set; }
        public int PointLevel { get; set; }
        public string PayCode { get; set; }
        public int CurrencyId { get; set; }
        public string PayCurrencyId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int AmountRule { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string AmountShow { get; set; }
        public IEnumerable<decimal> AmountList => AmountShow == null ? null : JsonConvert.DeserializeObject<IEnumerable<decimal>>(AmountShow);
        public int GSort { get; set; }
        public int SSort { get; set; }
    }

    public class PayGroup : IPaymentLimit
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
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
        public DepositType Type { get; set; }
        public int Sort { get; set; }
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
