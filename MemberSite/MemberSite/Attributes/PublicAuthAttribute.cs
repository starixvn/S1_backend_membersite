using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Security.Cryptography;

namespace MemberSite.Attributes;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PublicAuthAttribute : Attribute, IAuthorizationFilter
{
    public PublicAuthAttribute()
    {
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8);
        var content = reader.ReadToEndAsync().Result;
        var jbody = JObject.Parse(content);
        var request = JsonConvert.DeserializeObject<dynamic>(content);
        var token = jbody["Token"].ToString();
        request["Token"] = "1qaz2wsx";
        var requestStr = request.ToString();
        requestStr = requestStr.Replace(" \"", "\"").Replace("\r", "").Replace("\n", "");
        var checkToken = SHA256(requestStr);
        if (token == checkToken)
        {
            var a = "yes";
        }
        else
        {
            var b = "no";
        }
    }

    private string SHA256(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
        return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
    }

}

