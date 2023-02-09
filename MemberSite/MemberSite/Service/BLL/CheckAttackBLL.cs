using Amazon.S3.Model;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Mapping;
using MemberSite.Service.Model;
using MemberSite.Controllers.Api;

namespace MemberSite.Service.BLL
{
    public class CheckAttackBLL
    {
        private readonly ILogger<CheckAttackBLL> _logger;
        private readonly Tools _tools;
        private readonly IpHelper _ipHelper;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly ICacheHelper _cacheHelper;
        private readonly IPBLL _iPBll;
        private readonly IEnumerable<AreaCodeMap> _areaCodeMap;
        private readonly AWSS3Helper _s3Helper;
        private readonly string _fileName;
        private readonly string _folder;
        private readonly string _nginxIpKey;

        public CheckAttackBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CheckAttackBLL>>();
            _tools = provider.GetRequiredService<Tools>();
            _ipHelper = provider.GetRequiredService<IpHelper>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBll>();
            _cacheHelper = provider.GetRequiredService<ICacheHelper>();
            _iPBll = provider.GetRequiredService<IPBLL>();
            _areaCodeMap = provider.GetRequiredService<IEnumerable<AreaCodeMap>>();
            _s3Helper = provider.GetRequiredService<AWSS3Helper>();
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            _fileName = env.IsProduction() || env.EnvironmentName == "Release" ? "geoblockIp.txt" : "geoblockIp_test.txt";
            _nginxIpKey = env.IsProduction() || env.EnvironmentName == "Release" ? "$remote_addr" : "$real_ip";
            _folder = Path.Combine(env.ContentRootPath, "NginxConfig");

        }

        public bool CheckAttack(RrofileReq req, HttpRequest Request)
        {
            var clientIp = _tools.GetClientIP();
            if (_ipHelper.IpInRange(clientIp))
            {
                return false;
            }

            var serverInfo = _serverInfoBll.GetServerInfo();
            var checkCountForShortTime = _cacheHelper.StringIncrementAsync($"IP:{clientIp}", TimeSpan.FromMinutes(1)).Result;
            var checkCountForLongTime = _cacheHelper.StringIncrementAsync($"IP:10:{clientIp}", TimeSpan.FromMinutes(20)).Result;
            var checkCountForBlock = _cacheHelper.StringIncrementAsync($"IP:hour:{clientIp}", TimeSpan.FromMinutes(30)).Result;
            if (checkCountForShortTime >= 5 || checkCountForLongTime >= 15)
            {
                _cacheHelper.StringFireAndForgetSetAsync($"block:{clientIp}", string.Empty, TimeSpan.FromHours(1));
            }

            var isDart = Request.Headers["User-Agent"].ToString().Contains("Dart");

            if (checkCountForBlock >= 120 || isDart)
            {
                var reason = isDart ? "agent use Dart" : "too many requests";
                var memo = $"Auto Deny (/Site/ExistsPhoneNo) - Attack: get user phone number, reason is {reason}";
                var setResult = _iPBll.SetBlockIp(new BlockIpRequest { IP = clientIp, Memo = memo, Operator = "System" });
                if (setResult > 0 && !_cacheHelper.KeyExists($"AutoDeny:{clientIp}"))
                {
                    _cacheHelper.StringFireAndForgetSetAsync($"AutoDeny:{clientIp}", string.Empty, TimeSpan.FromMinutes(1));
                    _ = Task.Run(async () => { _ = await Upload(); });
                }
            }

            if (/*(serverInfo.SiteId is (int)SiteId.Bwing or (int)SiteId.Goal123 && req.AreaCode != "+84")*/
                isDart
                //|| _areaCodeMap.All(i => i.AreaCode != req.AreaCode)
                || checkCountForShortTime >= 5
                || checkCountForLongTime >= 15
                || checkCountForBlock >= 60
                || _cacheHelper.KeyExists($"block:{clientIp}"))
            {
                _logger.LogInformation($"Attack: AreaCode:{req.AreaCode}, PhoneNo:{req.PhoneNo}, REQUEST URL:{_serverInfoBll.GetRequestUrl()}, " +
                                       $"IP: {Request.Headers["X-Forwarded-For"]}, {Request.Headers["HTTP_X_FORWARDED_FOR"]}, {Request.Headers["HTTP_INCAP_CLIENT_IP"]}, {Request.Headers["HTTP_TRUE_CLIENT_IP"]}, {Request.Headers["HTTP_X_REAL_IP"]}, {Request.Headers["REMOTE_ADDR"]}, {Request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()}");
                return true;
            }

            return false;
        }

        public bool CheckAttackForSignIn(SiteController.LoginModel req, bool checkReferer, HttpRequest Request)
        {
            var clientIp = _tools.GetClientIP();
            if (_ipHelper.IpInRange(clientIp))
            {
                return false;
            }

            var checkCountForShortTime = 0;
            var checkCountForBlock = 0;
            // var checkCountForShortTime = _cacheHelper.StringIncrementAsync($"IP:SignIn:{clientIp}", TimeSpan.FromMinutes(1)).Result;
            // var checkCountForBlock = _cacheHelper.StringIncrementAsync($"IP:SignIn:long:{clientIp}", TimeSpan.FromMinutes(10)).Result;
            // if (checkCountForShortTime >= 20)
            // {
            //     _cacheHelper.StringFireAndForgetSetAsync($"block:SignIn:{clientIp}", string.Empty, TimeSpan.FromHours(1));
            // }

            var isAttack = string.Compare(req.platform, "x", StringComparison.OrdinalIgnoreCase) == 0 ||
                           string.Compare(req.userAgent, "x", StringComparison.OrdinalIgnoreCase) == 0 ||
                           string.Compare(req.screen, "x", StringComparison.OrdinalIgnoreCase) == 0;

            var isWrong = req.UserName.Contains("@") ||
                          (checkReferer && string.IsNullOrWhiteSpace(Request.Headers["Referer"].ToString()));

            if (checkCountForBlock >= 60 || isAttack)
            {
                var reason = isAttack ? "platform or userAgent or screen use x" : "too many requests";
                var memo = $"Auto Deny (/Site/SignIn or /Site/SignInApp) - Attack: try login, reason is {reason}";
                var setResult = _iPBll.SetBlockIp(new BlockIpRequest { IP = clientIp, Memo = memo, Operator = "System" });
                if (setResult > 0 && !_cacheHelper.KeyExists($"AutoDeny:SignIn:{clientIp}"))
                {
                    _cacheHelper.StringFireAndForgetSetAsync($"AutoDeny:SignIn:{clientIp}", string.Empty, TimeSpan.FromMinutes(1));
                    _ = Task.Run(async () => { _ = await Upload(); });
                }
            }

            if (isAttack
                || isWrong
                || checkCountForShortTime >= 20
                || checkCountForBlock >= 60
                || _cacheHelper.KeyExists($"block:SignIn:{clientIp}"))
            {
                _logger.LogInformation($"Attack: UserName:{req.UserName}, Userpwd:{req.Userpwd}, REQUEST URL:{_serverInfoBll.GetRequestUrl()}, " +
                                       $"IP: {Request.Headers["X-Forwarded-For"]}, {Request.Headers["HTTP_X_FORWARDED_FOR"]}, {Request.Headers["HTTP_INCAP_CLIENT_IP"]}, {Request.Headers["HTTP_TRUE_CLIENT_IP"]}, {Request.Headers["HTTP_X_REAL_IP"]}, {Request.Headers["REMOTE_ADDR"]}, {Request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()}");

                return true;
            }

            return false;
        }

        public bool CheckAttackForSignup()
        {
            var clientIp = _tools.GetClientIP();
            if (_ipHelper.IpInRange(clientIp))
            {
                return false;
            }

            var checkCountForShortTime = _cacheHelper.StringIncrementAsync($"IP:Signup:{clientIp}", TimeSpan.FromMinutes(10)).Result;
            var checkCountForBlock = _cacheHelper.StringIncrementAsync($"IP:Signup:long:{clientIp}", TimeSpan.FromHours(1)).Result;
            if (checkCountForShortTime >= 20)
            {
                _cacheHelper.StringFireAndForgetSetAsync($"block:Signup:{clientIp}", string.Empty, TimeSpan.FromHours(1));
            }

            if (checkCountForBlock >= 60)
            {
                var memo = $"Auto Deny (/Site/Signup) - Attack: try login, reason is too many requests";
                var setResult = _iPBll.SetBlockIp(new BlockIpRequest { IP = clientIp, Memo = memo, Operator = "System" });
                if (setResult > 0 && !_cacheHelper.KeyExists($"AutoDeny:Signup:{clientIp}"))
                {
                    _cacheHelper.StringFireAndForgetSetAsync($"AutoDeny:Signup:{clientIp}", string.Empty, TimeSpan.FromMinutes(1));
                    _ = Task.Run(async () => { _ = await Upload(); });
                }
            }

            if (checkCountForShortTime >= 20
                || checkCountForBlock >= 60
                || _cacheHelper.KeyExists($"block:Signup:{clientIp}"))
            {
                return true;
            }

            return false;
        }


        private async Task<bool> Upload()
        {
            try
            {
                var ipList = _iPBll.GetBlockIp<BlockIPForUpload>(true).Select(i => i.IP).ToList();
                Directory.CreateDirectory(_folder);
                var filePath = Path.Combine(_folder, _fileName);
                await File.WriteAllTextAsync(filePath, "#### Block List 0:open 1:block -1:default\r\n" +
                                                       $"geo {_nginxIpKey} $ipcheck " +
                                                       "{ \r\n" +
                                                       "default -1;\r\n");
                await File.AppendAllLinesAsync(filePath, ipList);
                await File.AppendAllTextAsync(filePath, "}\r\n" + 
                                                        "\r\n" +
                                                        "map $ip_geoblock:$ipcheck $geoblock {\r\n" +
                                                        "default $default_geoblock;\r\n" +
                                                        "0:0 0;\r\n" +
                                                        "0:1 0;\r\n" +
                                                        "0:-1 0;\r\n" +
                                                        "-1:1 1;\r\n" +
                                                        "}\r\n");
                using var destination = new MemoryStream();
                await using (var source = File.Open(filePath, FileMode.Open))
                {
                    await source.CopyToAsync(destination);
                }

                var putRequest = new PutObjectRequest
                {
                    BucketName = "ailab-config",
                    ContentType = "text/plain",
                    InputStream = destination,
                    Key = _fileName
                };
                return await _s3Helper.PutObjectAsync(putRequest);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Upload S3 Fail.");
                return false;
            }
        }
    }
}
