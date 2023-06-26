using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using System.Text.RegularExpressions;

namespace MemberSite.Service.BLL
{
    public class AffCodeObject
    {
        public string Value { get; private set; }

        public AffCodeObject(string affCode)
        {
            Value = affCode.Trim();
        }

        public bool IsValid() 
        {
            var _regexAffCode = new Regex(@"^[0-9]{5,20}$");
            return _regexAffCode.IsMatch(Value);
        }

        public bool IsExists(int siteId, Func<int, string, bool> IsExists) 
        {
            return IsExists(siteId, Value);
        }
    }
}
