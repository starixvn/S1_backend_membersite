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
                data.Title = JsonContentParser.Parse(data.Title, language.LanguageCode, language.DefaultLanguage);
                data.Content = JsonContentParser.Parse(data.Content, language.LanguageCode, language.DefaultLanguage);
                data.ImageURL = JsonContentParser.Parse(data.ImageURL, language.LanguageCode, language.DefaultLanguage);
            }

            return data;
        }

        public async Task<PageData<News>> GetNews(int siteId, LanguageObject language, int pageSize, int pageNumber)
        {
            var news = await newsService.GetNews(siteId, pageSize, pageNumber);
            foreach (var data in news.Data)
            {
                data.Title = JsonContentParser.Parse(data.Title, language.LanguageCode, language.DefaultLanguage);
                data.Content = JsonContentParser.Parse(data.Content, language.LanguageCode, language.DefaultLanguage);
                data.ImageURL = JsonContentParser.Parse(data.ImageURL, language.LanguageCode, language.DefaultLanguage);
            }

            return news;
        }
    }
}
