using System.Text.RegularExpressions;

using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class PromotionService
    {
        private readonly MySqlClient _mainDbClient;

        public PromotionService(IServiceProvider serviceProvider)
        {
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool SetDepositPromotionQueue(DepositPromotionQueue request)
        {
            return _mainDbClient.Execute("Member_DepositBonusQueue_Set_V2",
                new
                {
                    request.TransId,
                    request.BonusCode,
                    request.CustId,
                    request.SiteId,
                    request.UserName,
                    request.CurrencyId,
                    request.DeviceType,
                    request.LockedAmount,
                    IP = request.ClientIp
                }) > 0;
        }
        public bool CheckSpecialCode(string specialCode)
        {
            if (string.IsNullOrEmpty(specialCode) || specialCode.Length > 20)
                return false;
            var regex = new Regex("^[- !@#$%^&*a-zA-Z0-9]{1,20}$", RegexOptions.Singleline);
            var mPwd = regex.Match(specialCode);

            return mPwd.Success;
        }

        public bool CheckScratchCardCode(string specialCode)
        {
            if (string.IsNullOrEmpty(specialCode) || specialCode.Length != 12)
                return false;

            var regex = new Regex("^[a-zA-Z]{5}-[a-zA-Z0-9]{6}$", RegexOptions.Singleline);
            var mPwd = regex.Match(specialCode);

            return mPwd.Success;
        }
    }
}
