using Amazon.S3.Model;
using MemberSite.Extensions;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL;

public class CustPhotoIdBLL : ICustPhotoIdBLL
{
    private readonly ImageEditorBLL _imageEditorBll;
    private readonly ICustPhotoIdService _custPhotoIdService;
    private readonly AWSS3Helper _s3Helper;
    private readonly ILogger<CustPhotoIdBLL> _logger;
    private readonly string _environment;

    public CustPhotoIdBLL(ImageEditorBLL imageEditorBll, ICustPhotoIdService custPhotoIdService, AWSS3Helper s3Helper, IWebHostEnvironment env, ILogger<CustPhotoIdBLL> logger)
    {
        _imageEditorBll = imageEditorBll;
        _logger = logger;
        _custPhotoIdService = custPhotoIdService;
        _s3Helper = s3Helper;
        _environment = env.IsPrd() ? "PRD" : "DEV";
    }

    public async Task<long> UploadPhotoIdAsync(int custId, int siteId, IFormFile photo)
    {
        using var fileStream = photo.OpenReadStream();
        using var uploadedImage = await SixLabors.ImageSharp.Image.LoadAsync(fileStream);
        var fileBytes = await _imageEditorBll.Resize(uploadedImage);

        var fileName = photo.FileName;
        fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
        fileName = $"{fileName.Substring(0, fileName.LastIndexOf('.'))}.jpg";

        var sq = await _custPhotoIdService.SetCustPhotoId(custId, siteId, fileName, fileBytes);
        if (sq <= 0)
        {
            _logger.LogError("insert CustPhotoId failed");
            return 0;
        }
        var site = (SiteId)siteId;
        var siteName = site.ToString().ToUpper();
        var stream = new MemoryStream(fileBytes);
        var key = $"{_environment}/{siteName}/CustPhotoId/{custId}/{sq}.jpeg";
        var putRequest = new PutObjectRequest
        {
            BucketName = "ailab-backoffice",
            ContentType = "image/jpeg",
            InputStream = stream,
            Key = key
        };

        var isSuccess = await _s3Helper.PutObjectAsync(putRequest);
        return isSuccess ? sq : 0;
    }
}