using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using MemberSite.Service.Library;
using System.Data;
using MemberSite.Extensions;

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
        public IEnumerable<Msg> GetMsgList(int custId, LanguageObject language, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var msgList = _messageService.GetMsgList(custId, pageSize, ref pageNumber, out totalPages, out rowCount);
            foreach (var msg in msgList)
            {
                msg.Title = msg.Title.IsJson() ? JsonContentParser.ParseByJObject(msg.Title, language.LanguageCode, language.DefaultLanguage, "Title") : msg.Title;
            }
            return msgList;
        }

        public async Task<MsgContent> GetMsgContent(int custId, LanguageObject language, int msgId, int isBatch)
        {
            var msg = await _messageService.GetMsgContent(custId, msgId, isBatch);
            if (msg != null) 
            {
                msg.Content = msg.Content.IsJson() ? 
                    JsonContentParser.ParseByJObject(msg.Content, language.LanguageCode, language.DefaultLanguage, "Content") :
                    msg.Content;
            }
            return msg;
        }

        public async Task<PopupMsg> GetPopupMessage(int custId, LanguageObject language)
        {
            var popupMsg = await _messageService.GetPopupMessage(custId);
            popupMsg.PopupMessage = JsonContentParser.ParseByJObject(popupMsg.PopupMessage, language.LanguageCode, language.DefaultLanguage, "PopupMessage");
            return popupMsg;
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

        public Task<int> SendMessage(Message message) =>
            _messageService.SendMessageAsync(message);
    }
}
