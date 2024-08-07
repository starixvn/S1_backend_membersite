using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Store.Redemption;
using System.Data;
using System.Security.Policy;

namespace MemberSite.Service.DAL
{
    public class RedemptionService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportClient;

        public RedemptionService(IEnumerable<MySqlClient> mySqlClients)
        {
            _mainDbClient = mySqlClients.First(p => p.DataBase == DataBase.Main);
            _reportClient = mySqlClients.First(p => p.DataBase == DataBase.Report);
        }


        public async Task<string> GetRedemptionId(CreateRedemptionRequest req)
        {
            var p = new DynamicParameters();
            p.Add("_CustId", req.CustId, DbType.Int32);
            p.Add("_SiteId", req.SiteId, DbType.Int32);
            p.Add("_CurrencyId", req.CurrencyId, DbType.Int32);
            p.Add("_GiftId", req.GiftId, DbType.Int32);
            p.Add("_Point", req.Point, DbType.Int32);

            return await _mainDbClient.QueryFirstAsync<string>("Member_RedemptionTransId_Get", p);
        }

        public GiftInfo GetGiftInfo(CreateRedemptionRequest req)
        {
            var p = new DynamicParameters();
            p.Add("_GiftId", req.GiftId, DbType.Int32);

            return _reportClient.QueryFirst<GiftInfo>("Member_GiftList_Get",p);
        }


        public async Task<string> GetTpBonusCodeId(string bonusCode,string gameId)
        {
            var p = new DynamicParameters();
            p.Add("_BonusCode", bonusCode);
            p.Add("_GameId", gameId);


            return _reportClient.QueryFirst<string>("Member_TpBonusCodeId_Get", p);
        }
    }
}
