using Dapper;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class FaceBookPixelService
    {
        private readonly MySqlClient _mainDbClient;
        
        public FaceBookPixelService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public int CustFaceBookDataIns(CustFacebookData request)
        {
            var p = new DynamicParameters(new
            {
                _CustId = request.CustId,
                _TransId = request.TransId,
                _Ip = request.Ip,
                _Fbc = request.Fbc,
                _Fbp = request.Fbp,
                _UserAgent = request.UserAgent
            });
            
            return _mainDbClient.Execute("Service_CustFaceBookData_Ins", p );
        }
    }
}
