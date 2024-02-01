using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class QueueJobService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDb;
    
        public QueueJobService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
    
        public int InsertQueueJob(int custId, QueueJob type, string data)
        {
            var p = new DynamicParameters(new
            {
                _CustId = custId,
                _Type = (int) type,
                _Data = data
            });
            return _mainDbClient.Execute("Service_QueueJob_Ins", p);
        }
    
    }
}

