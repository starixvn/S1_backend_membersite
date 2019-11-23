using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Library;
using System.Data;

namespace MemberSite.Service.DAL
{
    public class DepoistService
    {
        private readonly IConfiguration configuration;
        private readonly Common common;

        public DepoistService(IConfiguration configuration, Common common)
        {
            this.configuration = configuration;
            this.common = common;
        }
        public int DepositReq(DepositDetailInfo depositDetailInfo, out string transId)
        {
            transId = "";
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                conn.Open();
                using (MySqlTransaction trans = conn.BeginTransaction())
                {
                    transId = conn.ExecuteScalar<string>("DepositInfo_Ins", new { DispositType = depositDetailInfo.DispositType, CustId = depositDetailInfo.CustId, UserName = depositDetailInfo.UserName, SiteId = depositDetailInfo.SiteId, DispositInfo = depositDetailInfo.DispositInfo, Amount = depositDetailInfo.Amount, Memo = depositDetailInfo.Memo, PostScript = depositDetailInfo.PostScript, Status = (int)DepositStatus.Pending, Prefix = $"D{common.GetEnv()}" }, transaction: trans, commandType: CommandType.StoredProcedure);
                    trans.Commit();
                    return 0;
                }
            }
        }
    }
}
