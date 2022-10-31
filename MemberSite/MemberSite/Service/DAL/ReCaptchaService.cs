using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL;

public class RecaptchaService
{
    private readonly MySqlClient _sqlClient;

    public RecaptchaService(IEnumerable<MySqlClient> sqlClients)
    {
        _sqlClient = sqlClients.SingleOrDefault(a => a.DataBase == DataBase.Report);
    }

    public async Task<string> GetGroup(string domain)
    {
        return await _sqlClient.QueryFirstAsync<string>("Member_RecaptchaGroup_Get", new { _Domain = domain });
    }
}