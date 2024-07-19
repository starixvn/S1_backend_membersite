using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class CommonService
    {
        private readonly MySqlClient _mainDbClient;
        public CommonService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool SetOTPLogCustInfo(int siteId,int custId,int currencyId,string userName,string otpNum)
        {
            var p = new DynamicParameters(new
            {
                SiteId  = siteId,
                CustId  = custId,
                CurrencyId  = currencyId,
                UserName  = userName,
                OTPNum  = otpNum,
            });
            var result = _mainDbClient.Execute("Service_OTPLogCustInfo_Set", p);

            return result >= 0 ;
        }
        
    }
}
