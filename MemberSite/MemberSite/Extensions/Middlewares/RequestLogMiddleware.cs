using System.Text;

using MemberSite.Service.BLL;
using MemberSite.Service.Library;

using Microsoft.AspNetCore.Http.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Serilog.Context;

namespace MemberSite.Extensions.Middlewares
{
    public class RequestLogMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly Tools _tools;
        private readonly IEnumerable<string> _markPwdKeys;
        private readonly IEnumerable<string> _markPwdPaths;

        public RequestLogMiddleware(RequestDelegate next, ILoggerFactory factory, AuthorizeBLL authorizeBll,
            ServerInfoBll serverInfoBll, Tools tools)
        {
            _next = next;
            _logger = factory.CreateLogger("RequestLog");
            _authorizeBll = authorizeBll;
            _serverInfoBll = serverInfoBll;
            _tools = tools;
            _markPwdKeys = new[] {"UserPwd", "OldPassword", "NewPassword"};
            _markPwdPaths = new[]
            {
                "/SignIn",
                "/SignInApp",
                "/Signup",
                "/ChangePassword"
            };
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();
            var url = context.Request.GetDisplayUrl();
            var path = context.Request.Path.Value;
            if (path == "/" && (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) ||
                                context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
            }
            else
            {
                var requestBodyStream = new MemoryStream();
                var originalRequestBody = context.Request.Body;
                var content = string.Empty;
                if (context.Request.ContentType?.StartsWith("application/x-www-form-urlencoded") ?? false)
                {
                    var formData = await context.Request.ReadFormAsync();
                    content = JsonConvert.SerializeObject(formData);
                }

                await context.Request.Body.CopyToAsync(requestBodyStream);
                requestBodyStream.Seek(0, SeekOrigin.Begin);

                using (var requestBody = new StreamReader(requestBodyStream, Encoding.UTF8, true, 1024, true))
                {
                    var codePage = requestBody.CurrentEncoding.CodePage;

                    content = !context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                              && string.IsNullOrEmpty(content)
                        ? await requestBody.ReadToEndAsync()
                        : content;

                    var authData = _authorizeBll.GetAuthData();
                    var serverInfo = _serverInfoBll.GetServerInfo();

                    foreach (var header in context.Request.Headers)
                    {
                        LogContext.PushProperty($"Headers.{header.Key}", header.Value.ToString());
                    }

                    LogContext.PushProperty("CustId", authData?.CustId);
                    LogContext.PushProperty("UserName", authData?.UserName);
                    LogContext.PushProperty("SiteId", serverInfo?.SiteId);
                    LogContext.PushProperty("ClientIp", _tools.GetClientIP());

                    // log需將密碼隱碼
                    if (path != null && _markPwdPaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        content = MarkLogPassword(content);
                    }

                    var log = new StringBuilder();
                    log.AppendLine($"REQUEST URL: {url}, Method: {context.Request.Method}, CodePage: {codePage},");
                    log.AppendLine($"REQUEST BODY: {content}");
                    _logger.LogInformation(log.ToString());
                    
                }

                requestBodyStream.Seek(0, SeekOrigin.Begin);
                context.Request.Body = requestBodyStream;

                await _next(context);
                context.Request.Body = originalRequestBody;
            }
        }

        private  string MarkLogPassword(string json)
        {
            var objKeys = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                .Select(x => x.Key)
                .ToList();
            
            var jObj = JObject.Parse(json);

            foreach (var key in _markPwdKeys)
            {
                var realKey = objKeys.FirstOrDefault(x => key.Equals(x, StringComparison.OrdinalIgnoreCase));
                
                if (string.IsNullOrEmpty(realKey))
                    continue;
                
                if (!string.IsNullOrEmpty(jObj[realKey]?.ToString()))
                {
                    jObj[realKey] = "********";
                }
            }

            return jObj.ToString();
        }
    }
}