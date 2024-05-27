using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class QuestionnaireBLL
    {
        private readonly QustionnaireService _qustionnaireService;

        public QuestionnaireBLL(QustionnaireService qustionnaireService)
        {
            _qustionnaireService = qustionnaireService;
        }


        public int Set(Questionnairerequest request,Customer customer)
        {
            return _qustionnaireService.Set(customer.SiteId, customer.CustId, customer.CurrencyId,request.Content,request.ContactInfo);
        }
    }
}