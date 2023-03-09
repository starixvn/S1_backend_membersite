using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class NewsBLL
    {
        private readonly NewsService newsService;

        public NewsBLL(NewsService newsService)
        {
            this.newsService = newsService;
        }

        public async Task<News> GetNewsById(int siteId, int newsId, LanguageObject language)
        {
            var data = await newsService.GetNewsById(siteId, newsId);
            if(data != null)
            {
                data.Title = JsonContentParser.ParseByLanguage(data.Title, language.LanguageCode, language.DefaultLanguage);
                data.Content = JsonContentParser.ParseByLanguage(data.Content, language.LanguageCode, language.DefaultLanguage);
                data.ImageURL = JsonContentParser.ParseByLanguage(data.ImageURL, language.LanguageCode, language.DefaultLanguage);
            }

            return data;
        }

        public IEnumerable<News> GetNews(int siteId, LanguageObject language, int pageSize, ref int pageNumber, out int totalPages, out int rowCount)
        {
            var datas = newsService.GetNews(siteId, pageSize, ref pageNumber, out totalPages, out rowCount);
            foreach (var data in datas)
            {
                data.Title = JsonContentParser.ParseByLanguage(data.Title, language.LanguageCode, language.DefaultLanguage);
                data.Content = JsonContentParser.ParseByLanguage(data.Content, language.LanguageCode, language.DefaultLanguage);
                data.ImageURL = JsonContentParser.ParseByLanguage(data.ImageURL, language.LanguageCode, language.DefaultLanguage);
            }

            return datas;
        }
    }
}
