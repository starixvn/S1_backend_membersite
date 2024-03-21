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

        public int IsExists(Func<string, int> IsExists)
        {
            CurrencyId = IsExists(Value);
            return CurrencyId;
        }
    }
}
