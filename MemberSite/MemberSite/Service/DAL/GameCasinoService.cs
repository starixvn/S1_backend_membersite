using Dapper;
using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class GameCasinoService
    {
        private readonly IConfiguration configuration;
        public GameCasinoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public List<GameCasinoListData> GetGameCasinoListByDevice(int deviceType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var IsStage = configuration.GetValue<int>("IsStage");
            var SPName = "Member_GameCasinoListByDevice_Get";
            if (IsStage == 1)
            {
                SPName = "Member_GameCasinoListByDeviceStag_Get";
            }
            return conn.Query<GameCasinoListData>(SPName, new { DeviceType = deviceType }, commandType: CommandType.StoredProcedure).ToList();
        }
    }
}
