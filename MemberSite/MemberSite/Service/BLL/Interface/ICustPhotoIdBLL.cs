namespace MemberSite.Service.BLL.Interface;

public interface ICustPhotoIdBLL
{
    Task<long> UploadPhotoIdAsync(int custId, int siteId, IFormFile photo);
}