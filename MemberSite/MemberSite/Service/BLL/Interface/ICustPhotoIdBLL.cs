namespace MemberSite.Service.BLL.Interface;

public interface ICustPhotoIdBLL
{
    Task<int> UploadPhotoIdAsync(int custId, int siteId, IFormFile photo);
}