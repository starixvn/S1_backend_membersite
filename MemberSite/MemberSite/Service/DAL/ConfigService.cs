using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class ConfigService
    {
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _reportIpTableClient;

        public ConfigService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _reportIpTableClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.ReportIPTable);
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null, DataBase dataBase = DataBase.Report)
        {
            return dataBase switch
            {
                DataBase.Report => _reportDbClient.Query<T>(sql, parameters),
                DataBase.ReportIPTable => _reportIpTableClient.Query<T>(sql, parameters),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
