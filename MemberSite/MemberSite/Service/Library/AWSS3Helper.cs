using Amazon.S3;
using Amazon.S3.Model;
using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Newtonsoft.Json;
using System.Net;

namespace MemberSite.Service.Library;

public class AWSS3Helper
{
    private readonly ILogger<AWSS3Helper> _logger;
    private readonly AWSPublicServiceUrl _publicServiceUrl;
    private readonly AmazonS3Client _s3Client;

    public AWSS3Helper(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        var _awsKeyConfig = configBll.GetConfigValue<AWSKeyConfig>("AWSS3Key");
        _publicServiceUrl = configBll.GetConfigValue<AWSPublicServiceUrl>("AWSPublicServiceUrl");
        var _accessKey = _awsKeyConfig.AccessKey;
        var _secretKey = _awsKeyConfig.SecretKey;

        var _amazonS3Config = new AmazonS3Config
        {
            ServiceURL = _publicServiceUrl.ExternalServiceUrl,
            ForcePathStyle = true,
            UseHttp = true
        };

        _s3Client = new AmazonS3Client(_accessKey, _secretKey, _amazonS3Config);
        _logger = provider.GetRequiredService<ILogger<AWSS3Helper>>();
    }

    public async Task<bool> PutObjectAsync(PutObjectRequest request)
    {
        var response = await _s3Client.PutObjectAsync(request);
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            _logger.LogInformation($"UploadS3File, Bucket={request.BucketName}, Key={request.Key} Response={JsonConvert.SerializeObject(response)}");
        }

        return response.HttpStatusCode == HttpStatusCode.OK;
    }
}