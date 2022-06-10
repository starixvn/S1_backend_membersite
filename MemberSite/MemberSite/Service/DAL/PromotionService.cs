using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class PromotionService
    {
        private readonly IConfiguration _configuration;

        public PromotionService(IServiceProvider serviceProvider)
        {
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
        }

        public bool SetDepositPromotionQueue(string transId, string bonusCode, int custId, int siteId, string userName, int currencyId, int deviceType, decimal lockedAmount, string custIp)
        {
            using var conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_DepositBonusQueue_Set_V2",
                new
                {
                    TransId = transId, BonusCode = bonusCode, CustId = custId, SiteId = siteId, UserName = userName,
                    CurrencyId = currencyId, DeviceType = deviceType, LockedAmount = lockedAmount, IP = custIp
                }, commandType: CommandType.StoredProcedure) > 0;
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
