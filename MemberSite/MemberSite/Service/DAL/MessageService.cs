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

        public async Task<bool> SetRead(int msgId, int custId)
        {
            return await _mainDbClient.ExecuteAsync("Member_MsgRead_Set_V3", new { MsgId = msgId, CustId = custId }) > 0;
        }

        public async Task<bool> SetReadPopupMessageTime(int custId, int taskId)
        {
            var p = new DynamicParameters(new { _CustId = custId, _TaskId = taskId });
            return await _mainDbClient.ExecuteAsync("Member_PopupMsgRead_Set", p) > 0;
        }

        public async Task<PageData<Msg>> GetMsgList(int custId, int pageSize, int pageNumber)
        {
            var p = new DynamicParameters(new { CustId = custId, PageSize = pageSize });
            p.Add("PageNumber", pageNumber, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("TotalPages", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("RowCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("StartIndex", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var msgList = await _reportClient.QueryAsync<Msg>("MsgList_Get_V3", p);

            return new PageData<Msg>() 
            {
                Data = msgList,
                PageNumber = p.Get<int?>("PageNumber") ?? 0,
                TotalPages = p.Get<int?>("TotalPages") ?? 0,
                RowCount = p.Get<int?>("RowCount") ?? 0
            };
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

        public async Task<bool> DelMsg(int custId, DeleteMsg msg)
        {
            return await _mainDbClient.ExecuteAsync("Msg_Del_V3", new { msg.MsgId, CustId = custId }) > 0;
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
