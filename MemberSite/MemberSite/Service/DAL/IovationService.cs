using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class IovationService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _logDbClient;
        public IovationService(IEnumerable<MySqlClient> dbClients)
        {
            _mainDbClient = dbClients.First(a => a.DataBase == DataBase.Main);
            _reportDbClient = dbClients.First(a => a.DataBase == DataBase.Report);
            _logDbClient = dbClients.First(a => a.DataBase == DataBase.Log);
        }
        public void SetIovation(int custId, string iovation, string actionType, string deviceId, string reason, string deviceType, int score)
        {
            var p = new DynamicParameters(new { CustId = custId, DeviceId = deviceId, Iovation = iovation, Reason = reason, DeviceType = deviceType,  Score = score });
            p.Add("IovationOLD", dbType: DbType.String, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_CustIovation_Set_V2", p);
            var iovationOLD = p.Get<string>("IovationOLD");
            //SetIovationChange
            if (result > 0)
            {
                _logDbClient.Execute("Service_LogIovationChange_Ins", new { CustId = custId, ActionType = actionType, Iovation = iovation, IovationOLD = iovationOLD });
            }
        }

        public CustIovation GetCustIovation(int custId)
        {
            var p = new DynamicParameters(new { _CustId = custId });
            var result = _reportDbClient.QueryFirst<CustIovation>("Member_CustIovation_Get", p);
            
            return result;
        } 
    }
}
