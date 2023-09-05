namespace MemberSite.Service.DAL.Interface
{
    public interface IAffiliateService
    {
        bool ExistsAffiliateCode(int siteId, string affCode);
    }
}