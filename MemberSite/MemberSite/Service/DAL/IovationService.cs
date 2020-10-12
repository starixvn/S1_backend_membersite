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
            var result = conn.Execute("Service_CustIovation_Set", new { CustId = custId, ActionType = actionType, Iovation = iovation }, commandType: CommandType.StoredProcedure);
            //SetIovationChange
            if (result > 0)
            {
                //Devid Yea
                using MySqlConnection connLog = new MySqlConnection(configuration.GetConnectionString("ConnectionStringLog"));
                connLog.Execute("Log", new { CustId = custId, ActionType = actionType, Iovation = iovation }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
