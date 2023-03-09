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
        public ApiListResult<News> GetNews(ParamNews newsReq)
        {
            var result = new ApiListResult<News>();

            try
            {
                int? currency = newsReq?.CurrencyId;

                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var customerInfo = _customerInfoBll.GetCustInfoWithCache(authData.CustId);
                    currency = customerInfo.CurrencyId;
                }
                var serverInfo = _serverInfoBll.GetServerInfo();
                var language = LanguageObject.Create(currency, _selLangBll.GetLangId(), serverInfo.DefLang);
                var pageNumber = newsReq.PageNumber;

                result.Data = _newsBLL.GetNews(serverInfo.SiteId, language, newsReq.PageSize, ref pageNumber, out var totalPages, out var rowCount);
                result.PageNumber = pageNumber;
                result.TotalPages = totalPages;
                result.RowCount = rowCount;
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

        public ApiResult<News> GetNewsById(ParamNewsById newsReq)
        {
            var result = new ApiResult<News>();

            try
            {
                int? currency = newsReq?.CurrencyId;

                var authData = _authorizeBll.GetAuthData();
                if (authData is { CustId: > 0 })
                {
                    var customerInfo = _customerInfoBll.GetCustInfoWithCache(authData.CustId);
                    currency = customerInfo.CurrencyId;
                }
                var serverInfo = _serverInfoBll.GetServerInfo();
                var language = LanguageObject.Create(currency, _selLangBll.GetLangId(), serverInfo.DefLang);

                result.Data = _newsBLL.GetNewsById(serverInfo.SiteId, newsReq.NewsId, language);
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
