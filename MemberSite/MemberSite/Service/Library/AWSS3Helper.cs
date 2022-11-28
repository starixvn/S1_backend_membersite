using System.Net;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MemberSite.Service.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace MemberSite.Service.Library;

public class AWSS3Helper
{
    private readonly IAmazonS3 _client;
        private readonly ILogger<AWSS3Helper> _logger;

        public AWSS3Helper(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var awsKeyConfig = provider.GetRequiredService<IOptions<Dictionary<string, AWSKeyConfig>>>().Value["ailab-s3"];
            var proxyHost = configuration.GetValue<string>("ProxyHost");
            var proxyPort = configuration.GetValue<int>("ProxyPort");

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.APSoutheast1
            };

            if (!string.IsNullOrEmpty(proxyHost))
            {
                config.ProxyHost = proxyHost;
                config.ProxyPort = proxyPort;
            }

            _client = new AmazonS3Client(awsKeyConfig.AccessKey, awsKeyConfig.SecretKey, config);
            _logger = provider.GetRequiredService<ILogger<AWSS3Helper>>();
        }

        public async Task<bool> PutObjectAsync(PutObjectRequest request)
        {
            var response = await _client.PutObjectAsync(request);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation($"UploadS3File, Bucket={request.BucketName}, Key={request.Key} Response={JsonConvert.SerializeObject(response)}");
            }

            return response.HttpStatusCode == HttpStatusCode.OK;
        }
}