using MemberSite.Service.BLL.Interface;

namespace MemberSite.Service.BLL;

public class CustPhotoIdBLL : ICustPhotoIdBLL
{
    private readonly ImageEditorBLL _imageEditorBll;
    private readonly CustInfoBll _custInfoBLL;
    private readonly ILogger<CustPhotoIdBLL> _logger;

    public CustPhotoIdBLL(ImageEditorBLL imageEditorBll, CustInfoBll custPhoto, ILogger<CustPhotoIdBLL> logger)
    {
        _imageEditorBll = imageEditorBll;
        _custInfoBLL = custPhoto;
        _logger = logger;
    }

    public async Task<long> UploadPhotoIdAsync(int custId, int siteId, IFormFile photo)
    {
        using var fileStream = photo.OpenReadStream();
        using var uploadedImage = await SixLabors.ImageSharp.Image.LoadAsync(fileStream);
        var fileBytes = await _imageEditorBll.Resize(uploadedImage);

        var fileName = photo.FileName;
        fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
        fileName = $"{fileName.Substring(0, fileName.LastIndexOf('.'))}.jpg";

        var sq = await _custInfoBLL.SetCustPhotoId(custId, siteId, fileName, fileBytes);
        if (sq <= 0)
        {
            _logger.LogError("insert CustPhotoId failed");
            return 0;
        }
        return sq;
    }
}