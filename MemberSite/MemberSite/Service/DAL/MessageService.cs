using System.Data;
using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class MessageService
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportClient;

        public MessageService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public bool SetRead(int msgId, int custId, bool isBatch)
        {
            return _mainDbClient.Execute("Member_MsgRead_Set_V2", new { MsgId = msgId, CustId = custId, IsBatch = isBatch }) > 0;
        }

        public async Task<bool> SetReadPopupMessageTime(int custId, int taskId)
        {
            var p = new DynamicParameters(new { _CustId = custId, _TaskId = taskId });
            return await _mainDbClient.ExecuteAsync("Member_PopupMsgRead_Set", p) > 0;
        }

        public IEnumerable<Msg> GetMsgList(int custId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            totalPages = 0;
            
            var p = new DynamicParameters(new { CustId = custId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var msgList = _reportClient.Query<Msg>("MsgList_Get_V3", p);
            totalPages = p.Get<int?>("TotalPages") ?? 0;
            pageNumber = p.Get<int?>("PageNumber") ?? 0;
            rowCount = p.Get<int?>("RowCount") ?? 0;
            return msgList;
        }
        
        public async Task<MsgContent> GetMsgContent(int custId, int msgId, int isBatch)
        {
            var msgContent = await  _mainDbClient.QueryFirstAsync<MsgContent>(
                "MsgContent_Get_V3",
                new { MsgId = msgId, CustId = custId, IsBatch = isBatch });
            return msgContent;
        }

        public async Task<PopupMsg> GetPopupMessage(int custId, int pageSize = 5)
        {
            var pageNumber = 0;
            var p = new DynamicParameters(new { _CustId = custId, _PageSize = pageSize });
            p.Add("_PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.Output);
            var msg = await _reportClient.QueryFirstAsync<PopupMsg>("Member_PopupMsg_Get_V5", p);
            if (msg == null) return null;
            pageNumber = p.Get<int>("_PageNumber");
            msg.PageNumber = pageNumber;
            return msg;
        }

        public bool DelMsg(int custId, DeleteMsg msg)
        {
            return _mainDbClient.Execute("Msg_Del_V2", new { msg.MsgId, CustId = custId, msg.IsBatch }) > 0;
        }
        
        public async Task<int> GetMsgCount(int custId)
        {
            return await _reportClient.ExecuteScalarAsync<int>("MsgCount_Get_V3", new { CustId = custId });
        }

        public IEnumerable<Announcement> GetAnnouncement(string LangCode, string DefaultLangCode, int LimitRow, bool IsTest, int SiteId)
        {
            return _reportClient.Query<Announcement>("Member_Announcement_Get", new { DefaultLangCode, LangCode, LimitRow, IsTest, SiteId });
        }

        public Task<int> SendMessageAsync(Message message) =>
            _mainDbClient.ExecuteAsync("Msg_Ins_V4", new DynamicParameters(message));
    }
}
