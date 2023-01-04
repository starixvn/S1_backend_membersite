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

namespace MemberSite.Attributes;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PublicAuthAttribute : Attribute, IAuthorizationFilter
{
    private readonly Tools _tools;
    private readonly AuthBLL _authBll;
    public PublicAuthAttribute(Tools tools, AuthBLL authBll)
    {
        _tools = tools;
        _authBll = authBll;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var fromUrl = _tools.GetFromUrl();
        var deviceType = _tools.GetDeviceId(fromUrl);

        //App 先不用驗證
        if (deviceType != DeviceId.APP)
        {
            using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
            var content = reader.ReadToEndAsync().Result;
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
        }
    }

    private bool IsJsonResult(string ContentType)
    {
        return ContentType != null && (ContentType.Contains("*/*") || ContentType.Contains("application/json"));
    }
}

