using System.Text.RegularExpressions;

namespace MemberSite.Service.Domain
{
    public class ReferralObject
    {
        public string Value { get; private set; }
        public int CurrencyId { get; private set; }

        public ReferralObject(string affCode)
        {
            Value = affCode.Trim();
        }

        public bool IsValid()
        {
            var _regexAffCode = new Regex(@"^[a-z0-9]{8}$");
            return _regexAffCode.IsMatch(Value);
        }

        public bool IsExists(int siteId, Func<int, string, (bool IsExist, int CurrencyId)> IsExists)
        {
            var result = IsExists(siteId, Value);
            CurrencyId = result.CurrencyId;
            return result.IsExist;
        }
    }
}
