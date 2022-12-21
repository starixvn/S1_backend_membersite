using System.Data;
using Dapper;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;

namespace MemberSite.Service.DAL;

public class CustPhotoIdService : ICustPhotoIdService
{
    private readonly MySqlClient _mainDbClient;

    public CustPhotoIdService(MySqlClient mainDbClient)
    {
        _mainDbClient = mainDbClient;
    }

    public async Task<long> SetCustPhotoId(int custId, int siteId, string picFileName, byte[] photoIdPic)
    {
        var p = new DynamicParameters();
        p.Add("_CustId", custId);
        p.Add("_SiteId", siteId);
        p.Add("_PicFileName", picFileName);
        p.Add("_PhotoIdPic", photoIdPic);
        p.Add("_SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);

        var result = await _mainDbClient.ExecuteAsync("Member_CustPhotoId_Set_V2", p);
        if (result <= 0) return 0;
        
        var sq = p.Get<long>("_SQ");
        return sq > 0 ? sq : 0;
    }
}