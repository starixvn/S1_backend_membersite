namespace MemberSite.Service.DAL.Interface;

public interface ICustPhotoIdService
{
    Task<long> SetCustPhotoId(int custId, int siteId);
}