using Dapper;
using MemberSite.Service.Model;
using MySql.Data.MySqlClient;
using System.Data;
using MemberSite.Service.Library;
using Newtonsoft.Json;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class IPService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDb;
        public IPService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public async Task<IEnumerable<BlockIP>> GetBlockIP()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return await conn.QueryAsync<BlockIP>("Member_BlockIP_Get", commandType: CommandType.StoredProcedure);
        }
    }
}
