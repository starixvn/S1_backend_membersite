using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
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

        public RequestLogMiddleware(RequestDelegate next, ILoggerFactory factory, AuthorizeBLL authorizeBll,
            ServerInfoBLL serverInfoBll)
        {
            _next = next;
            _logger = factory.CreateLogger("RequestLog");
            _authorizeBll = authorizeBll;
            _serverInfoBll = serverInfoBll;
        }

        public async Task Invoke(HttpContext context)
        {
            var url = context.Request.GetDisplayUrl();
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
                _logger.LogInformation(
                    $"REQUEST METHOD: {context.Request.Method}, CodePage: {codePage}, REQUEST BODY: {content}, REQUEST URL: {url}, ES: {context.Request.Cookies[".ES"]}, Referer: {context.Request.Headers["Referer"]}");
            }

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            await _next(context);
            context.Request.Body = originalRequestBody;
        }
    }
}