using MemberSite.Service.BLL.Interface;

namespace MemberSite.Service.BLL;

public class CustPhotoIdBLL : ICustPhotoIdBLL
{
    private readonly ImageEditorBLL _imageEditorBll;
    private readonly CustInfoBll _custInfoBLL;

    public CustPhotoIdBLL(ImageEditorBLL imageEditorBll, CustInfoBll custPhoto)
    {
        _imageEditorBll = imageEditorBll;
        _custInfoBLL = custPhoto;
    }

    public async Task<int> UploadPhotoIdAsync(int custId, int siteId, IFormFile photo)
    {
        using var fileStream = photo.OpenReadStream();
        using var uploadedImage = await SixLabors.ImageSharp.Image.LoadAsync(fileStream);
        var fileBytes = await _imageEditorBll.Resize(uploadedImage);

        var fileName = photo.FileName;
        fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
        fileName = $"{fileName.Substring(0, fileName.LastIndexOf('.'))}.jpg";

        var result = await _custInfoBLL.SetCustPhotoId(custId, siteId, fileName, fileBytes);
        return result;
    }
}