using MemberSite.Service.Model;
using System.Text.RegularExpressions;

namespace MemberSite.Service.BLL
{
    public class AffCodeValidate
    {
        private readonly Regex _regexAffCode = new(@"^[0-9]{5,20}$");
        private readonly ISelLang _selLangBll;

        public AffCodeValidate(ISelLang selLangBll)
        {
            _selLangBll = selLangBll;
        }

        public (int errorCode, string message) Validate(string affCode) 
        {
            if (!_regexAffCode.IsMatch(affCode))
            {
                return (-9, _selLangBll.GetMsg("msg_AffCodeValidate"));
            }
            return (0, "");
        }
    }
}
