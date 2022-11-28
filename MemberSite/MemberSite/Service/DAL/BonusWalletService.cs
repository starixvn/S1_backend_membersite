using System.Data;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using MySql.Data.MySqlClient;

namespace MemberSite.Service.DAL
{
    public class BonusWalletService
    {
        
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly int typePosition = 1000;

        public BonusWalletService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<BonusWallet> GetCustBonusWallet(int CustId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var datas = conn.Query<BonusWallet>("Member_CustBonus_Get_V2", new {CustId},
                commandType: CommandType.StoredProcedure);
            
            foreach (var data in datas)
            {
                data.Turnover = data.Turnover < 0 ? 0 : data.Turnover;
            }

            return datas;
        }

        public BonusWallet CheckBonusWallet(int CustId, int TpId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonus_Check", new { CustId, TpId }, commandType: CommandType.StoredProcedure);
        }
        public BonusWallet CheckBonusWallet(int CustId, string bonusCode)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonusByCode_Get", new { CustId, BonusCode = bonusCode }, commandType: CommandType.StoredProcedure);
        }

        public BonusWallet CustBonusInfo(int CustId, string BonusCode, int TpId, int PromoCategoryId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.QueryFirstOrDefault<BonusWallet>("Member_CustBonus_GetById", new { CustId, BonusCode, TpId, PromoCategoryId }, commandType: CommandType.StoredProcedure);
        }
    }
}
