using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers.Api
{
    [Route("api/[controller]/[action]/{LangId}")]
    [ApiController]
    public class NewsController : Controller
    {
        private readonly ServerInfoBll serverInfoBLL;
        private readonly NewsBLL newsBLL;

        public NewsController(ServerInfoBll serverInfoBLL, NewsBLL newsBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.newsBLL = newsBLL;
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
                var serverInfo = serverInfoBLL.GetServerInfo();
                var pageNumber = newsReq.PageNumber;
                result.Data = newsBLL.GetNews(serverInfo.SiteId, newsReq.CurrencyId, newsReq.PageSize, ref pageNumber, out var totalPages, out var rowCount);
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
                var serverInfo = serverInfoBLL.GetServerInfo();
                result.Data = newsBLL.GetNewsById(serverInfo.SiteId, newsReq.CurrencyId, newsReq.NewsId);
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
