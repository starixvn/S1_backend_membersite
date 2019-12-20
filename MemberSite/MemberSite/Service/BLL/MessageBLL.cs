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
    }
}
