using System.Data;
using MySql.Data.MySqlClient;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class MessageService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportClient;

        public MessageService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public bool SetRead(int msgId, int custId, bool isBatch)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Member_MsgRead_Set_V2", new { MsgId = msgId, CustId = custId, IsBatch = isBatch }, commandType: CommandType.StoredProcedure) > 0;
        }

        public async Task<bool> SetReadPopupMessageTime(int custid, int taskId)
        {
            var p = new DynamicParameters(new { _CustId = custid, _TaskId = taskId });
            return await _mainDbClient.ExecuteAsync("Member_PopupMsgRead_Set", p) > 0;
        }

        public IEnumerable<Msg> GetMsgList(int custId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            totalPages = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            var p = new DynamicParameters(new { CustId = custId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var msgList = conn.Query<Msg>("MsgList_Get_V2", p, commandType: CommandType.StoredProcedure);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            pageNumber = p.Get<int?>("PageNumber") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return msgList;
        }
        public MsgContent GetMsgContent(int custId, int msgId, int isBatch)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var msgContent = conn.QueryFirstOrDefault<MsgContent>("MsgContent_Get_V2", new { MsgId = msgId, CustId = custId, IsBatch = isBatch }, commandType: CommandType.StoredProcedure);
            return msgContent;
        }

        public async Task<PopupMsg> GetPopupMessage(int custId, int pageSize = 5)
        {
            var pageNumber = 0;
            var p = new DynamicParameters(new { _CustId = custId, _PageSize = pageSize });
            p.Add("_PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.Output);
            var msg = await _reportClient.QueryFirstAsync<PopupMsg>("Member_PopupMsg_Get_V3", p);
            if (msg == null) return null;
            pageNumber = p.Get<int>("_PageNumber");
            msg.PageNumber = pageNumber;
            return msg;
        }

        public bool DelMsg(int CustId, DeleteMsg msg)
        {
            return _mainDbClient.Execute("Msg_Del_V2", new { msg.MsgId, CustId, msg.IsBatch }) > 0;
        }
        public int GetMsgCount(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.ExecuteScalar<int>("MsgCount_Get", new { CustId = custId, IsUnread = 1 }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<Announcement> GetAnnouncement(string LangCode, string DefaultLangCode, int LimitRow, bool IsTest, int SiteId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringReport"));
            return conn.Query<Announcement>("Member_Announcement_Get", new { DefaultLangCode, LangCode, LimitRow, IsTest, SiteId }, commandType: CommandType.StoredProcedure);
        }
        public int SendMessage(int siteId, string sender, string title, string content, int custId, string CustUserName, string lang)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Msg_Ins", new { Type = 99, SiteId = siteId, LangId = lang, RecipientCustId = custId, Recipient = CustUserName, SenderCustId = siteId, Sender = sender, Title = title, Content = content, MainId = 0 }, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> SendMessageAsync(Message message)
        {
            var parameter = new DynamicParameters();
            parameter.Add("Type", 99);
            parameter.Add("SiteId", message.SiteId);
            parameter.Add("LangId", message.LangId);
            parameter.Add("RecipientCustId", message.CustId);
            parameter.Add("Recipient", message.CustUserName);
            parameter.Add("SenderCustId", message.SiteId);
            parameter.Add("Sender", message.Sender);
            parameter.Add("Title", message.Title);
            parameter.Add("Content", message.Content);
            parameter.Add("MainId", 0);
            return await _mainDbClient.ExecuteAsync("Msg_Ins", parameter);
        }
    }
}
