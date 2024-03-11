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

        public RequestLogMiddleware(RequestDelegate next, ILoggerFactory factory, AuthorizeBLL authorizeBll,
            ServerInfoBll serverInfoBll, Tools tools)
        {
            _next = next;
            _logger = factory.CreateLogger("RequestLog");
            _authorizeBll = authorizeBll;
            _serverInfoBll = serverInfoBll;
            _tools = tools;
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
                        LogContext.PushProperty(header.Key, header.Value.ToString());
                    }

                    LogContext.PushProperty("CustId", authData?.CustId);
                    LogContext.PushProperty("UserName", authData?.UserName);
                    LogContext.PushProperty("SiteId", serverInfo?.SiteId);
                    LogContext.PushProperty("ClientIp", _tools.GetClientIP());
                    using (LogContext.PushProperty("Domain", context.Request.Host.ToString()))
                    using (LogContext.PushProperty("ClientAgent", context.Request.Headers["User-Agent"].ToString()))
                    using (LogContext.PushProperty("Referer", context.Request.Headers["Referer"].ToString()))
                    {
                        // log需將登入密碼隱碼
                        if (path.Contains("/api/Site/SignIn", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains("/api/Site/SignInApp", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains("/api/Site/Signup", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains("/api/Site/SignupApp", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains("/api/Extra/Signup", StringComparison.OrdinalIgnoreCase) ||
                            path.Contains("/api/Account/ChangePassword", StringComparison.OrdinalIgnoreCase))
                        {
                            var jbody = JObject.Parse(content);
                            if (jbody.TryGetValue("Userpwd", StringComparison.OrdinalIgnoreCase, out var pwd)
                                && !string.IsNullOrEmpty(pwd.ToString()))
                            {
                                jbody["Userpwd"] = "********";
                            }
                            if (jbody.TryGetValue("OldPassword", StringComparison.OrdinalIgnoreCase, out var opwd)
                               && !string.IsNullOrEmpty(opwd.ToString()))
                            {
                                jbody["OldPassword"] = "********";
                            }
                            if (jbody.TryGetValue("NewPassword", StringComparison.OrdinalIgnoreCase, out var npwd)
                               && !string.IsNullOrEmpty(npwd.ToString()))
                            {
                                jbody["NewPassword"] = "********";
                            }
                            content = jbody.ToString();
                        }

                        var log = new StringBuilder();
                        log.AppendLine($"REQUEST URL: {url}, Method: {context.Request.Method}, CodePage: {codePage},");
                        log.AppendLine($"REQUEST BODY: {content}");
                        _logger.LogInformation(log.ToString());
                    }
                }

                requestBodyStream.Seek(0, SeekOrigin.Begin);
                context.Request.Body = requestBodyStream;

                await _next(context);
                context.Request.Body = originalRequestBody;
            }
        }
    }
}