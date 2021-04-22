using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace MemberSite.Service.BLL
{
    public class MessageBLL
    {
        private readonly MessageService messageService;
        private readonly IMemoryCache cache;

        public MessageBLL(MessageService messageService, IMemoryCache cache)
        {
            this.messageService = messageService;
            this.cache = cache;
        }
        public IEnumerable<Msg> GetMsgList(int custId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            totalPages = 0;
            return messageService.GetMsgList(custId, pageSize, ref pageNumber, out totalPages, out rowCount);
        }

        public MsgContent GetMsgContent(int custId, int msgId)
        {
            return messageService.GetMsgContent(custId, msgId);
        }
        public bool SetMsgRead(int custId, int[] msgIds)
        {
            bool resalt = true;
            foreach (var msgId in msgIds)
            {
                resalt &= messageService.SetRead( msgId, custId);
            }

            return resalt;
        }
        public bool DelMsg(int custId, int[] msgIds)
        {
            bool resalt = true;
            foreach (var msgId in msgIds)
            {
                resalt &= messageService.DelMsg(custId, msgId);
            }

            return resalt;
        }

        public int GetMsgCount(int custId)
        {
            return messageService.GetMsgCount(custId);
        }

        public IEnumerable<Announcement> GetAnnouncement(string LangCode, string DefaultLangCode, int LimitRow, bool IsTest, int SiteId)
        {
            string key = $"Announcement_{SiteId}_{LangCode}";
            var cacheData = cache.Get<IEnumerable<Announcement>>(key);
            if (cacheData == null)
            {
                cacheData = messageService.GetAnnouncement(LangCode, DefaultLangCode, LimitRow, IsTest, SiteId);

                if (cacheData != null)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(3));
                    cache.Set(key, cacheData, cacheEntryOptions);
                }
            }

            return cacheData;
        }
        public int SendMessage(int siteId, string sender, string title, string content, int custId, string CustUserName, string lang)
        {
            return messageService.SendMessage(siteId, sender, title, content, custId, CustUserName, lang);
        }

        public async void SendMessage(Message message)
        {
            await messageService.SendMessageAsync(new Message(message.SiteId, message.Sender, message.Title, message.Content, message.CustId,
                message.CustUserName, message.LangId));
        }
    }
}
