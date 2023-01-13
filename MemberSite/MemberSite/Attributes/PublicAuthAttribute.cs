using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Security.Cryptography;
using MemberSite.Service.Library;
using MemberSite.Service.BLL;
using MemberSite.Service.Model.enums;
using Serilog.Context;
using System.IO;
using Microsoft.AspNetCore.Http.Extensions;

namespace MemberSite.Attributes;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PublicAuthAttribute : Attribute, IAuthorizationFilter
{
    private readonly Tools _tools;
    private readonly AuthBLL _authBll;
    private readonly IHostEnvironment _environment;
    public PublicAuthAttribute(Tools tools, AuthBLL authBll, IHostEnvironment environment)
    {
        _tools = tools;
        _authBll = authBll;
        _environment = environment;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        //var fromUrl = _tools.GetFromUrl();
        var fromUrl = context.HttpContext.Request.GetDisplayUrl();
        var deviceType = _tools.GetDeviceId(fromUrl);

        //App 先不用驗證
        if (!_environment.IsDevelopment() && deviceType != DeviceId.APP)
        {
            //using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
            //var content = reader.ReadToEnd();
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.HttpContext.Request.Body;
            var content = string.Empty;

            if (context.HttpContext.Request.ContentType?.StartsWith("application/x-www-form-urlencoded") ?? false)
            {
                var formData = context.HttpContext.Request.ReadFormAsync().Result;
                content = JsonConvert.SerializeObject(formData);
            }

            context.HttpContext.Request.Body.CopyTo(requestBodyStream);
            requestBodyStream.Seek(0, SeekOrigin.Begin);

            using (var requestBody = new StreamReader(requestBodyStream, Encoding.UTF8, true, 1024, true))
            {
                var codePage = requestBody.CurrentEncoding.CodePage;

                content = !context.HttpContext.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                          && string.IsNullOrEmpty(content)
                    ? requestBody.ReadToEnd()
                    : content;
            }

            if (!_authBll.RequestAuth(content))
            {
                if (IsJsonResult(context.HttpContext.Request.ContentType))
                {
                    var response = context.HttpContext.Response;
                    response.ContentType = "application/json";
                    context.Result = new JsonResult(new { errorCode = 403, message = "Permission denied." });
                }
                else
                {
                    var result = new ContentResult
                    {
                        Content = "Permission denied."
                    };
                    context.Result = result;
                }
            }

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.HttpContext.Request.Body = requestBodyStream;

            //context.HttpContext.Request.Body = originalRequestBody;
        }
    }

    private bool IsJsonResult(string ContentType)
    {
        return ContentType != null && (ContentType.Contains("*/*") || ContentType.Contains("application/json"));
    }
}

