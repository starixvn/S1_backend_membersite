using System.Data;

using Dapper;

using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class QustionnaireService
    {
        private readonly MySqlClient _mainDbClient;

        public QustionnaireService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public int Set(int siteId,int custId,int currencyId,string content,string contactInfo)
        {
            var p = new DynamicParameters();
            p.Add("_SiteId", siteId);
            p.Add("_CustId", custId);
            p.Add("_Currency", currencyId);
            p.Add("_Content",content);
            p.Add("_ContactInfo", contactInfo);

            var result =  _mainDbClient.QueryFirst<int>("Member_Questionnaire_Set", p);

            return result;
        }
    }
}

    
