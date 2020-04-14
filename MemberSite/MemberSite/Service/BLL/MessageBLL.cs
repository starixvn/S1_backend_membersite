using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class MessageBLL
    {
        private readonly MessageService messageService;

        public MessageBLL(MessageService messageService)
        {
            this.messageService = messageService;
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

        public IEnumerable<Announcement> GetAnnouncement(int SiteId, string LangCode, string DefaultLangCode, int LimitRow, bool IsTest)
        {
            return messageService.GetAnnouncement(SiteId, LangCode, DefaultLangCode, LimitRow, IsTest);
        }
    }
}
