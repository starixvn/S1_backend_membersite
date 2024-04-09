using System.Data;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class CupEventService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public CupEventService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        #region 隊伍設定

        /// <summary>
        /// 取得後台使用者設定的世界盃設定
        /// </summary>
        /// <param name="langId">語系Id，看前端傳什語系</param>
        /// <returns></returns>
        public IEnumerable<CupEventPredictionSettingDto> GetCupEventPredictionSetting(string langId)
        {
            var p = new DynamicParameters();
            p.Add("LangId", langId, dbType: DbType.String);
            return _mainDbClient.Query<CupEventPredictionSettingDto>("Admin_CupEventPredictionSetting_Get", p);
        }

        /// <summary>
        /// 取得世界盃所有國家 Id 與名稱
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CupEventPredictionOptionsDto> GetCupEventPredictionOptions(string langId)
        {
            var p = new DynamicParameters();
            p.Add("LangId", langId, dbType: DbType.String);
            return _mainDbClient.Query<CupEventPredictionOptionsDto>("Admin_CupEventPredictionOptions_Get", p);
        }

        #endregion

        #region 充值 & 發票

        /// <summary>
        /// 取得玩家活動內存款清單
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MemberDepositDto> GetMemberDeposit(int siteId, int custId ,int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId, dbType: DbType.Int64);
            p.Add("_SiteId", siteId, dbType: DbType.Int64);
            p.Add("_CurrencyId", currencyId, dbType: DbType.Int64);
            return _reportDbClient.Query<MemberDepositDto>("Service_CupEventPromotionMissionDeposit_Get", p);
        }

        /// <summary>
        /// 取得玩家的流水總額
        /// </summary>
        /// <param name="custId"></param>
        /// <param name="siteId"></param>
        /// <param name="currencyId"></param>
        /// <param name="tpCategoryId"></param>
        /// <returns></returns>
        public decimal GetCupEventPromotionMissionTurnover(int siteId,int custId,int currencyId,int tpCategoryId)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId, dbType: DbType.Int64);
            p.Add("_SiteId", siteId, dbType: DbType.Int64);
            p.Add("_CurrencyId", currencyId, dbType: DbType.Int64);
            p.Add("_TpCategoryId", tpCategoryId, dbType: DbType.Int64);
            return _reportDbClient.QueryFirst<decimal>("Service_CupEventPromotionMissionTurnover_Get", p);
        }
        
        /// <summary>
        /// 新增玩家票券
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<int> InsertCupEventMemberTicket(CustTicketInfoDto data)
        {
            return _mainDbClient.ExecuteAsync("Service_CupEventMemberTicketInfo_Ins", new
            {
                _SiteId = data.SiteId,
                _CustId = data.CustId,
                _CurrencyId = data.CurrencyId,
                _TicketType = data.TicketType,
                _TicketStatus = data.TicketStatus,
                _TransId = data.TransId
            });
        }
        
        /// <summary>
        /// 取得玩家的票券
        /// </summary>
        /// <param name="custId"></param>
        /// <returns></returns>
        public IEnumerable<CupEventMemberTicketInfoDto> GetCupEventMemberTicketInfo(int custId)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId, dbType: DbType.Int64);
            return _reportDbClient.Query<CupEventMemberTicketInfoDto>("Member_CupEventMemberTicketInfo_Get", p);
        }

        /// <summary>
        /// 取得符合規定賽事的下注單
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="custId"></param>
        /// <param name="currencyId"></param>
        /// <param name="betAmountLimit"></param>
        /// <returns></returns>
        public IEnumerable<CupEventSportBet> GetCupEventSportBet(int siteId, int custId ,int currencyId , decimal betAmountLimit)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId, dbType: DbType.Int64);
            p.Add("_CustId", custId, dbType: DbType.Int64);
            p.Add("_CurrencyId", currencyId, dbType: DbType.Int64);
            p.Add("_BetAmountLimit", betAmountLimit, dbType: DbType.Int64);
            return _reportDbClient.Query<CupEventSportBet>("Service_CupEventSportBetCheck_Get", p);
        }
        
        /// <summary>
        /// 取得世界盃充值，投票設定時間
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CupEventDateSettingDto> GetCupEventDateSetting()
        {
            return _reportDbClient.Query<CupEventDateSettingDto>("Service_CupEventDateSetting_Get");
        }

        public Task<int> UserPollTicketAsync(int custId, UserPollTicketRequest request , MySqlTransaction trans)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", custId, dbType: DbType.Int64);
            p.Add("_TicketType", request.TicketType, dbType: DbType.Byte);
            p.Add("_TeamId", request.CountryId, dbType: DbType.Int64);

            return _mainDbClient.ExecuteAsync("Member_PollTicketInfo_Set",trans, p);
        }

        public IEnumerable<CupEventAffInfo> GetCupEventAffInfo(int siteId, int custId, int currencyId)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId, dbType: DbType.Int64);
            p.Add("_CustId", custId, dbType: DbType.Int64);
            p.Add("_CurrencyId", currencyId, dbType: DbType.Int64);
            return _reportDbClient.Query<CupEventAffInfo>("Service_CupEventAffInfo_Get" ,p);
        }
        
        #endregion
        
        
    }
}
