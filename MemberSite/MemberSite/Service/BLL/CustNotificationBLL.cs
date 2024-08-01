using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class CustNotificationBLL
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public CustNotificationBLL(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public async Task<IEnumerable<string>> GetCustNotification(int NotificationType, int CustId)
        {
            return await _reportDbClient.QueryAsync<string>("Service_CustNotification_Check", new { NotificationType , CustId });
        }

        public async Task<int> SetCustNotification(int NotificationType, int CustId, string TransId)
        {
            return await _mainDbClient.ExecuteAsync("Service_CustNotification_Ins", new { NotificationType, CustId, TransId });
        }


    }
}
