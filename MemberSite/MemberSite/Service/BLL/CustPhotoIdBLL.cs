using Amazon.S3.Model;
using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;

namespace MemberSite.Service.BLL;

public class CustPhotoIdBLL : ICustPhotoIdBLL
{
    private readonly ImageEditorBLL _imageEditorBll;
    private readonly ICustPhotoIdService _custPhotoIdService;
    private readonly AWSS3Helper _s3Helper;
    private readonly ServerInfoBll _serverInfoBll;
    private readonly ILogger<CustPhotoIdBLL> _logger;
    private readonly string _backOfficeBucketName;

    public CustPhotoIdBLL(ImageEditorBLL imageEditorBll,
        ICustPhotoIdService custPhotoIdService,
        AWSS3Helper s3Helper,
        ServerInfoBll serverInfoBll,
        IWebHostEnvironment env,
        ILogger<CustPhotoIdBLL> logger,
        ConfigBLL configBll)
    {
        _imageEditorBll = imageEditorBll;
        _logger = logger;
        _serverInfoBll = serverInfoBll;
        _custPhotoIdService = custPhotoIdService;
        _s3Helper = s3Helper;
        _backOfficeBucketName = configBll.GetConfigValue("AWSBackOfficeBucketName");
    }

    public async Task<long> UploadPhotoIdAsync(int custId, int siteId, IFormFile photo)
    {
        await using var fileStream = photo.OpenReadStream();
        using var uploadedImage = await SixLabors.ImageSharp.Image.LoadAsync(fileStream);
        var fileBytes = await _imageEditorBll.Resize(uploadedImage);

        var sq = await _custPhotoIdService.SetCustPhotoId(custId, siteId);
        if (sq <= 0)
        {
            _logger.LogError("insert CustPhotoId failed");
            return 0;
        }

        var serverInfo = _serverInfoBll.GetServerInfo();
        var siteName = serverInfo.SiteName;
        var stream = new MemoryStream(fileBytes);
        var key = $"{siteName}/CustPhotoId/{custId}/{sq}.jpeg";
        var putRequest = new PutObjectRequest
        {
            BucketName = _backOfficeBucketName,
            ContentType = "image/jpeg",
            InputStream = stream,
            Key = key
        };

        var isSuccess = await _s3Helper.PutObjectAsync(putRequest);
        return isSuccess ? sq : 0;
    }
}