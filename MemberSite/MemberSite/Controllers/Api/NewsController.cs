using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class NewsController : Controller
    {
        private readonly SelLangBLL _selLangBll;
        private readonly ServerInfoBll _serverInfoBll;
        private readonly AuthorizeBLL _authorizeBll;
        private readonly CustInfoBll _customerInfoBll;
        private readonly NewsBLL _newsBLL;
        public NewsController(SelLangBLL selLangBll, ServerInfoBll serverInfoBll, AuthorizeBLL authorizeBll, CustInfoBll customerInfoBll, NewsBLL newsBLL)
        {
            _selLangBll = selLangBll;
            _serverInfoBll = serverInfoBll;
            _authorizeBll = authorizeBll;
            _customerInfoBll = customerInfoBll;
            _newsBLL = newsBLL;
        }

        public class ParamNews
        {
            public int? CurrencyId { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }


        [HttpPost]
        public async Task<ApiListResult<News>> GetNews(ParamNews newsReq)
        {
            var result = new ApiListResult<News>();

            try
            { 
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var customerInfo = _customerInfoBll.GetCustInfoWithCache(authData.CustId);
                    newsReq.CurrencyId = customerInfo.CurrencyId;
                }
                var serverInfo = _serverInfoBll.GetServerInfo();
                var language = LanguageObject.Create(newsReq.CurrencyId, _selLangBll.GetLangId(), serverInfo.DefLang);
                var pageNumber = newsReq.PageNumber;

                var news = await _newsBLL.GetNews(serverInfo.SiteId, language, newsReq.PageSize, pageNumber);

                result.Data = news.Data;
                result.PageNumber = news.PageNumber;
                result.TotalPages = news.TotalPages;
                result.RowCount = news.RowCount;
                result.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.Message = ex.ToString();
            }

            return result;
        }


        public class ParamNewsById
        {
            public int? CurrencyId { get; set; }
            public int NewsId { get; set; }
        }

        public async Task<ApiResult<News>> GetNewsById(ParamNewsById newsReq)
        {
            var result = new ApiResult<News>();

            try
            {
                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var customerInfo = _customerInfoBll.GetCustInfoWithCache(authData.CustId);
                    newsReq.CurrencyId = customerInfo.CurrencyId;
                }
                var serverInfo = _serverInfoBll.GetServerInfo();
                var language = LanguageObject.Create(newsReq.CurrencyId, _selLangBll.GetLangId(), serverInfo.DefLang);

                result.Data = await _newsBLL.GetNewsById(serverInfo.SiteId, newsReq.NewsId, language);
                result.ErrorCode = 0;
            }
            catch (Exception ex)
            {
                result.ErrorCode = 999;
                result.Message = ex.ToString();
            }

            return result;
        }
    }
}
