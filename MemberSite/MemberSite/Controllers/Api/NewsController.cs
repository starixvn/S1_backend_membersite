using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ServerInfoBLL serverInfoBLL;
        private readonly NewsBLL newsBLL;

        public NewsController(ServerInfoBLL serverInfoBLL, NewsBLL newsBLL)
        {
            this.serverInfoBLL = serverInfoBLL;
            this.newsBLL = newsBLL;
        }

        public class ParamNews
        {
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
        }


        [HttpPost]
        public ApiListResult<News> GetNews(ParamNews NewsReq)
        {
            var result = new ApiListResult<News>();

            try
            {
                var serverInfo = serverInfoBLL.GetServerInfo();
                var pageNumber = NewsReq.PageNumber;
                int totalPages;
                int rowCount;
                result.Data = newsBLL.GetNews(serverInfo.SiteId, NewsReq.PageSize, ref pageNumber, out totalPages, out rowCount);
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
            public int NewsId { get; set; }
        }

        public ApiResult<News> GetNewsById(ParamNewsById NewsReq)
        {
            var result = new ApiResult<News>();

            try
            {
                var serverInfo = serverInfoBLL.GetServerInfo();
                result.Data = newsBLL.GetNewsById(serverInfo.SiteId, (int)NewsReq.NewsId);
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
