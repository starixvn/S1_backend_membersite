using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class NewsBLL
    {
        private readonly SelLangBLL selLangBLL;
        private readonly NewsService newsService;
        private Dictionary<int, string> SpecialLangMap;

        public NewsBLL(NewsService newsService, SelLangBLL selLangBLL)
        {
            this.newsService = newsService;
            this.selLangBLL = selLangBLL;
            SpecialLangMap = new Dictionary<int, string>() 
            {
                { 4, "th"},
                { 15, "id"},
                { 61, "hi"}
            };
        }

        private string ParseJSON(string langId, string jsonStr)
        {
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonStr);
            json.TryGetValue(langId, out var result);
            return result;
        }

        public News GetNewsById(int siteId, int? currencyId, int NewsId)
        {
            var data = newsService.GetNewsById(siteId, NewsId);
            if(data != null)
            {
                var langId = selLangBLL.GetLangId();
                if (currencyId.HasValue && SpecialLangMap.TryGetValue(currencyId.Value, out var lang) && langId == "en")
                {
                    langId = $"{lang}_en";
                }
                data.Title = ParseJSON(langId, data.Title);
                data.Content = ParseJSON(langId, data.Content);
                data.ImageURL = ParseJSON(langId, data.ImageURL);
            }

            return data;
        }

        public IEnumerable<News> GetNews(int siteId, int? currencyId, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var datas = newsService.GetNews(siteId, pageSize, ref pageNumber, out totalPages, out rowCount);
            var langId = selLangBLL.GetLangId();

            if (currencyId.HasValue && SpecialLangMap.TryGetValue(currencyId.Value, out var lang) && langId == "en") 
            {
                langId = $"{lang}_en";
            }

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
