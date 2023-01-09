using MemberSite.Service.Model;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class GameCasinoService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _reportDbClient;
        
        public GameCasinoService(IServiceProvider provider,IConfiguration configuration)
        {
            this.configuration = configuration;
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        
        public List<GameCasinoListData> GetGameCasinoListByDevice(int deviceType)
        {
            var isStage = configuration.GetValue<int>("IsStage");
            var spName = "Member_GameCasinoListByDevice_Get";
            
            if (isStage == 1)
            {
                spName = "Member_GameCasinoListByDeviceStag_Get";
            }
            
            return _reportDbClient.Query<GameCasinoListData>(spName, new { DeviceType = deviceType }).ToList();
        }
    }
}
