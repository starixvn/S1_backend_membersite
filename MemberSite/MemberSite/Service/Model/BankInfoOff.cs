using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class BankInfoOff : IPaymentLimit
    {
        public int BankId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public string IFSCCode { get; set; }
        public int SysId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int PointLevel { get; set; }
        public string Memo { get; set; }
        public int BSort { get; set; }
        public int CurrencyId { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string BankLogo { get; set; }
        public string BankSite { get; set; }
        public int Sort { get; set; }
        public int Purpose { get; set; }
        public int Ver { get; set; }
        public int Status { get; set; }
    }
    
    public class OfflineBankAccountInfo : IPaymentLimit
    {
        public int BankId { get; set; }
        public string AccountName { get; set; }
        public string AccountNo { get; set; }
        public string AccountCode { get; set; }
        public int SysId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal DailyLimitAmount { get; set; }
        public string StrPointLevels { get; set; }
        public IEnumerable<int> PointLevels =>
            JsonConvert.DeserializeObject<IEnumerable<int>>(StrPointLevels);
        
        public string StrVipLevels { get; set; }
        public IEnumerable<int> VipLevels =>
            JsonConvert.DeserializeObject<IEnumerable<int>>(StrVipLevels);
        
        public string Memo { get; set; }
        public int BSort { get; set; }
        public int CurrencyId { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string BankLogo { get; set; }
        public string BankSite { get; set; }
        public int Sort { get; set; }
        public string Purpose { get; set; }

        public IEnumerable<OfflineBankAccountPurposeEnum> Purposes =>
            JsonConvert.DeserializeObject<IEnumerable<OfflineBankAccountPurposeEnum>>(Purpose ?? "[]");
        
        public int Ver { get; set; }
        public int Status { get; set; }

        public string StrSupportChannel { get; set; }
        public IEnumerable<OfflineBankAccountSupportChannelEnum> SupportChannel =>
            JsonConvert.DeserializeObject<IEnumerable<OfflineBankAccountSupportChannelEnum>>(StrSupportChannel);

        public string QRCodeUrl { get; set; }
        public string StrGroupIds { get; set; }
        
        public IEnumerable<int> GroupIds =>
            JsonConvert.DeserializeObject<IEnumerable<int>>(StrGroupIds ?? "[]");
    }

    public class OfflineBankAccountDailyDeposit
    {
        public int BankId { get; set; }
        
        public decimal DailyDeposit { get; set; }
    }
    
    public class LocalPaySetting
    {
        public int CurrencyId { get; set; }
        
        public int GroupId { get; set; }
        
        public string GroupName { get; set; }
        
        public DepositType Type { get; set; }
    }
}
