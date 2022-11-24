using MemberSite.Service.Model;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class IPService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDb;
        public IPService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDb = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<BlockIP> GetBlockIp(bool forceUseMainDb)
        {
            const string spName = "Member_BlockIP_Get";
            return forceUseMainDb
                ? _mainDbClient.Query<BlockIP>(spName)
                : _reportDb.Query<BlockIP>(spName);
        }
        
        public int SetBlockIp(BlockIpRequest request)
        {
            return _mainDbClient.Execute("Admin_BlockIP_Set", new { request.IP, request.Operator, request.Memo });
        }
    }
}
