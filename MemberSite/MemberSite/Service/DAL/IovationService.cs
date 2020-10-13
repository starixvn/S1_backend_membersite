using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.DAL
{
    public class IovationService
    {
        private readonly IConfiguration configuration;

        public IovationService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public void SetIovation(int custId, string iovation, string actionType)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, ActionType = actionType, Iovation = iovation });
            p.Add("IovationOLD", dbType: DbType.String, direction: ParameterDirection.Output);
            var result = conn.Execute("Service_CustIovation_Set", p, commandType: CommandType.StoredProcedure);
            string iovationOLD = p.Get<string>("IovationOLD");
            //SetIovationChange
            if (result > 0)
            {
                using MySqlConnection connLog = new MySqlConnection(configuration.GetConnectionString("ConnectionStringLog"));
                connLog.Execute("Service_LogIovationChange_Ins", new { CustId = custId, ActionType = actionType, Iovation = iovation, IovationOLD = iovationOLD }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
