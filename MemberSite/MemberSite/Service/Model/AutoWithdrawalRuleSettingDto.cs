using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class AutoWithdrawalRuleSettingDto
    {
        public decimal WithdrawalAmount { get; set; }

        public int AccountWithdrawalCount { get; set; }

        public string VipLevelStr { get; set; }

        public string TagStr { get; set; }

        public string WithdrawalTypeStr { get; set; }

        public List<int> VipLevels =>
            string.IsNullOrEmpty(this.VipLevelStr)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(VipLevelStr);

        public List<int> Tags =>
            string.IsNullOrEmpty(this.TagStr)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(TagStr);

        public List<int> WithdrawalTypes => 
            string.IsNullOrEmpty(this.WithdrawalTypeStr)
            ? new List<int>()
            : JsonConvert.DeserializeObject<List<int>>(WithdrawalTypeStr);
    }
}