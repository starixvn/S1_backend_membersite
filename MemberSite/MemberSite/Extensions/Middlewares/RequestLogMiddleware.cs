using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace MemberSite.Extensions.Middlewares
{
    public class RequestLogMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly ServerInfoBLL _serverInfoBll;
        private readonly Tools _tools;

        public RequestLogMiddleware(RequestDelegate next, ILoggerFactory factory, AuthorizeBLL authorizeBll,
            ServerInfoBLL serverInfoBll, Tools tools)
        {
            _next = next;
            _logger = factory.CreateLogger("RequestLog");
            _authorizeBll = authorizeBll;
            _serverInfoBll = serverInfoBll;
            _tools = tools;
        }

        public async Task Invoke(HttpContext context)
        {
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
                    LogContext.PushProperty("CustId", authData?.CustId);
                    LogContext.PushProperty("UserName", authData?.UserName);
                    LogContext.PushProperty("SiteId", serverInfo?.SiteId);
                    LogContext.PushProperty("ClientIp", _tools.GetClientIP());
                    using (LogContext.PushProperty("Domain", context.Request.Host.ToString()))
                    using (LogContext.PushProperty("ClientAgent", context.Request.Headers["User-Agent"].ToString()))
                    using (LogContext.PushProperty("Referer", context.Request.Headers["Referer"].ToString()))
                    {
                        _logger.LogInformation(
                            $"REQUEST METHOD: {context.Request.Method}, CodePage: {codePage}, REQUEST BODY: {content}, REQUEST URL: {url}, ES: {context.Request.Cookies[".ES"]}, JWT: {context.Request.Headers?["authorization"].ToString()}");
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