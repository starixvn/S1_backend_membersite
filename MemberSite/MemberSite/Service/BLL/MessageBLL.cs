using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL
{
    public class MessageBll
    {
        private readonly MessageService _messageService;
        private readonly CacheHelper _cacheHelper;

        public MessageBll(IServiceProvider provider)
        {
            _messageService = provider.GetRequiredService<MessageService>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
        }
        public IEnumerable<Msg> GetMsgList(int custId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            totalPages = 0;
            return _messageService.GetMsgList(custId, pageSize, ref pageNumber, out totalPages, out rowCount);
        }

        public MsgContent GetMsgContent(int custId, int msgId, int isBatch)
        {
            return _messageService.GetMsgContent(custId, msgId, isBatch);
        }

        public async Task<PopupMsg> GetPopupMessage(int custId)
        {
            return await _messageService.GetPopupMessage(custId);
        }

        public bool SetMsgRead(int custId, List<DeleteMsg> list)
        {
            var result = true;
            foreach (var item in list)
            {
                result &= _messageService.SetRead(item.MsgId, custId, item.IsBatch);
            }

            return result;
        }

        public async Task<bool> SetPopupMsgRead(int custId)
        {
            return await _messageService.SetReadPopupMessageTime(custId, 1);
        }


        public bool DelMsg(int custId, List<DeleteMsg> list)
        {
            var result = true;
            foreach (var msg in list)
            {
                result &= _messageService.DelMsg(custId, msg);
            }

            return result;
        }

        public async Task<int> GetMsgCount(int custId) =>
            await _messageService.GetMsgCount(custId);

        public IEnumerable<Announcement> GetAnnouncement(string LangCode, string DefaultLangCode, int LimitRow, bool IsTest, int SiteId)
        {
            var key = $"Announcement_{SiteId}_{LangCode}_{DefaultLangCode}_{IsTest}";
            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(3)),
                i => _messageService.GetAnnouncement(LangCode, DefaultLangCode, LimitRow, IsTest, SiteId));
        }
        public Task<int> SendMessage(string sender, int custId, string lang, string title, string content)
        {
            return _messageService.SendMessage(sender, custId, lang, title, content);
        }

        public async void SendMessage(Message message)
        {
            await _messageService.SendMessageAsync(new Message(message.SiteId, message.Sender, message.Title, message.Content, message.CustId,
                message.CustUserName, message.LangId));
        }
    }
}
