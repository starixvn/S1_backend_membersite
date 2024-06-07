using Dapper;

using MemberSite.Extensions;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class PartitionService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public PartitionService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public List<TResult> MonthlyQuery<TResult>(DateTime StartDate, DateTime endDate, string spName, object parameters, DataBase dataBase = DataBase.Report)
        {
            var tasks = new List<Task<IEnumerable<TResult>>>();
            foreach (var partitionKey in StartDate.GetMonthlyPartitionRange(endDate))
            {
                var p = new DynamicParameters(parameters);
                p.Add("YYYYMM", partitionKey);
                var task = dataBase switch
                {
                    DataBase.Report => _reportDbClient.QueryAsync<TResult>(spName, p),
                    DataBase.Main => _mainDbClient.QueryAsync<TResult>(spName, p),
                    _ => throw new NotImplementedException(),
                };
                tasks.Add(task);
            }

            Task.WhenAll(tasks);

            var result = new List<TResult>();
            foreach (var item in tasks)
            {
                result.AddRange(item.Result);
            }
            return result;
        }
        
        public IEnumerable<TpBetRecord> GetTpBetRecord(DateTime startTime, DateTime endTime, string spName, object parameters, DataBase dataBase = DataBase.Report)
        {
            var result = Enumerable.Empty<TpBetRecord>();

            foreach (var partitionKey in startTime.GetTpBetRecordPartitionRange(endTime))
            {
                var p = new DynamicParameters(parameters);
                p.Add("_YYYYMM", partitionKey);
                var data = dataBase switch
                {
                    DataBase.Report => _reportDbClient.Query<TpBetRecord>(spName, p),
                    DataBase.Main => _mainDbClient.Query<TpBetRecord>(spName, p),
                    _ => throw new NotImplementedException(),
                };
                result = result.Concat(data);
            }

            return result;
        }
    }
}
