using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class NewsBLL
    {
        private readonly SelLangBLL selLangBLL;
        private readonly NewsService newsService;

        public NewsBLL(NewsService newsService, SelLangBLL selLangBLL)
        {
            this.newsService = newsService;
            this.selLangBLL = selLangBLL;
        }

        private string ParseJSON(string langId, string jsonStr)
        {
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonStr);
            json.TryGetValue(langId, out var result);
            return result;
        }

        public News GetNewsById(int SiteId, int NewsId)
        {
            var data = newsService.GetNewsById(SiteId, NewsId);
            if(data != null)
            {
                var langId = selLangBLL.GetLangId();
                data.Title = ParseJSON(langId, data.Title);
                data.Content = ParseJSON(langId, data.Content);
                data.ImageURL = ParseJSON(langId, data.ImageURL);
            }

            return data;
        }

        public IEnumerable<News> GetNews(int SiteId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var datas = newsService.GetNews(SiteId, pageSize, ref pageNumber, out totalPages, out rowCount);
            var langId = selLangBLL.GetLangId();

            foreach (var data in datas)
            {
                data.Title = ParseJSON(langId, data.Title);
                data.Content = ParseJSON(langId, data.Content);
                data.ImageURL = ParseJSON(langId, data.ImageURL);
            }

            return datas;
        }
    }
}
