using System.Data;

using Dapper;

using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class WorldCupPredictionService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;

        public WorldCupPredictionService(IServiceProvider provider)
        {
            this._configuration = provider.GetRequiredService<IConfiguration>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main); ;
        }

        #region 隊伍設定

        /// <summary>
        /// 取得後台使用者設定的世界盃設定
        /// </summary>
        /// <param name="langId">語系Id，看前端傳什語系</param>
        /// <returns></returns>
        public IEnumerable<WorldCupPredictionSettingDto> GetWorldCupPredictionSetting(string langId)
        {
            var p = new DynamicParameters();
            p.Add("LangId", langId, dbType: DbType.String);
            return _mainDbClient.Query<WorldCupPredictionSettingDto>("Admin_WorldCupPredictionSetting_Get", p);
        }

        /// <summary>
        /// 取得世界盃所有國家 Id 與英文名稱
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WorldCupPredictionOptionsDto> GetWorldCupPredictionOptions()
        {
            return _mainDbClient.Query<WorldCupPredictionOptionsDto>("Admin_WorldCupPredictionOptions_Get");
        }

        #endregion

        #region 充值 & 發票

        /// <summary>
        /// 取得玩家每日充值總計
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MemberDepositAmountDailyDto> GetMemberDepositAmountDaily(int siteId, int custId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId, dbType: DbType.Int64);
            p.Add("_CustId", custId, dbType: DbType.Int64);
            return _mainDbClient.Query<MemberDepositAmountDailyDto>("Member_MemberDepositAmountDaily_Get", p);
        }

        /// <summary>
        /// 取得世界盃衝值設定金額表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DepositAmountSettingDto> GetDepositAmountSetting()
        {
            return _mainDbClient.Query<DepositAmountSettingDto>("Member_DepositAmountSetting_Get");
        }

        /// <summary>
        /// 取得玩家的票券
        /// </summary>
        /// <param name="custId"></param>
        /// <returns></returns>
        public IEnumerable<WorldCupMemberTicketInfoDto> GetWorldCupMemberTicketInfo(int custId)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId, dbType: DbType.Int64);
            return _mainDbClient.Query<WorldCupMemberTicketInfoDto>("Member_WorldCupMemberTicketInfo_Get", p);
        }

        /// <summary>
        /// 取得世界盃充值，投票設定時間
        /// </summary>
        /// <returns></returns>
        public IEnumerable<WorldCupDateSettingDto> GetWorldCupDateSetting()
        {
            return _mainDbClient.Query<WorldCupDateSettingDto>("Service_WorldCupDateSetting_Get");
        }

        public Task UserPollTicketAsync(int custId, UserPollTicketRequest request)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId, dbType: DbType.Int64);
            p.Add("_TicketType", request.TicketType, dbType: DbType.Byte);
            p.Add("_TeamId", request.CountryId, dbType: DbType.Int64);

            return _mainDbClient.ExecuteAsync("Member_PollTicketInfo_Set", p);
        }

        #endregion
    }
}
