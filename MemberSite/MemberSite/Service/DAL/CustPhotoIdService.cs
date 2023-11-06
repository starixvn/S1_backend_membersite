using System.Data;

using Dapper;

using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL;

public class CustPhotoIdService : ICustPhotoIdService
{
    private readonly MySqlClient _mainDbClient;

    public CustPhotoIdService(IEnumerable<MySqlClient> mainDbClient)
    {
        _mainDbClient = mainDbClient.SingleOrDefault(a => a.DataBase == DataBase.Main);
    }

    public async Task<long> SetCustPhotoId(int custId, int siteId)
    {
        var p = new DynamicParameters();
        p.Add("_CustId", custId);
        p.Add("_SiteId", siteId);
        p.Add("_SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
        await _mainDbClient.ExecuteAsync("Member_CustPhotoId_Set_V3", p);

        var sq = p.Get<long>("_SQ");
        return sq > 0 ? sq : 0;
    }
}