using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MemberSite.Service.Model;

namespace MemberSite.Service.DAL
{
    public class MessageService
    {
        private readonly IConfiguration configuration;

        public MessageService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int GetCount(int custId, bool isUnread)
        {
            int result = 0;
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            result = conn.ExecuteScalar<int>("MsgCount_Get", new { CustId = custId, IsUnread = isUnread }, commandType: CommandType.StoredProcedure);
            return result;
        }
        public bool SetRead(int msgId, int custId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("MsgRead_Set", new { MsgId = msgId, CustId = custId }, commandType: CommandType.StoredProcedure) > 0;
        }

        public IEnumerable<Msg> GetMsgList(int custId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            totalPages = 0;

            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var msgList = conn.Query<Msg>("MsgList_Get", p, commandType: CommandType.StoredProcedure);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            pageNumber = p.Get<int?>("PageNumber") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return msgList;
        }
        public MsgContent GetMsgContent(int custId, int msgId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var msgContent = conn.QueryFirstOrDefault<MsgContent>("MsgContent_Get", new { MsgId = msgId, CustId = custId }, commandType: CommandType.StoredProcedure);
            return msgContent;
        }
        public bool DelMsg(int custId, int msgId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Msg_Del", new { MsgId = msgId, CustId = custId }, commandType: CommandType.StoredProcedure) > 0;
        }
    }
}
