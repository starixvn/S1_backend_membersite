using System.Net;
using System.Xml.Linq;

namespace MemberSite.Service.Library
{
    public interface IHttpHelper
    {
        Task<(TResult Result, HttpStatusCode StatusCode)> GetAsync<TResult>(string url, string query = null) where TResult : class;

        (TResult Result, HttpStatusCode StatusCode) Post<TBody, TResult>(string url, TBody body)
            where TResult : class;
        (TResult Result, HttpStatusCode StatusCode) FormPost<TBody, TResult>(string url, TBody body) 
            where TResult : class;

        (TResult Result, HttpStatusCode StatusCode) JsonPost<TBody, TResult>(string url, TBody data)
            where TResult : class;
        IHttpHelper WithHeader(string key, string value);
    }
}