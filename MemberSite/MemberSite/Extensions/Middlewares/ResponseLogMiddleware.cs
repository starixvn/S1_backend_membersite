using System.Diagnostics;
using System.Text;

using Serilog.Context;

namespace MemberSite.Extensions.Middlewares
{
    public class ResponseLogMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public ResponseLogMiddleware(RequestDelegate next, ILoggerFactory factory)
        {
            _next = next;
            _logger = factory.CreateLogger("ResponseLog");
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var path = context.Request.Path.Value;
            if (path == "/" && (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) ||
                                context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
            }
            else
            {
                var bodyStream = context.Response.Body;

                var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                await _next(context);

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var recordContent = await new StreamReader(responseBodyStream, Encoding.UTF8, true, 1024, true).ReadToEndAsync();

                // GetSlotGamePage回傳內容太長會超過ELK限制，Log擷取前面200字元
                if (path.Contains("api/Game/GetSlotGamePage", StringComparison.OrdinalIgnoreCase))
                {
                    recordContent = $"{recordContent[200..]} || api/Game/GetSlotGamePage Content exceeds the length limit.";
                }
                    
                using (LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds))
                {
                    _logger.LogInformation($"StatusCode: {context.Response.StatusCode}, RESPONSE: {recordContent}");
                }

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(bodyStream);
            }
        }
    }
}