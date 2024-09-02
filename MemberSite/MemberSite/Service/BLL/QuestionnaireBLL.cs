using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace MemberSite.Service.BLL
{
    public class QuestionnaireBLL
    {
        private readonly QustionnaireService _qustionnaireService;
        private readonly ICacheHelper _redisHelper;
        private readonly DateTimeHelper _dateTimeHelper;

        public QuestionnaireBLL(IServiceProvider provider)
        {
            _qustionnaireService = provider.GetRequiredService<QustionnaireService>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
            _dateTimeHelper = provider.GetRequiredService<DateTimeHelper>();
        }


        public async Task<int> Set(Questionnairerequest request,Customer customer)
        {
            var time =await _dateTimeHelper.ToCurrencyZone(DateTime.UtcNow, customer.CurrencyId); ;
            var key = $"Questionnaire:{time:yyyyMMdd}:{customer.CustId}";
            var cnt = _redisHelper.TryGetOrCreate(key, TimeSpan.FromDays(1), () => 0, RedisDataBase.Shared);

            if (cnt >= 3)
            {
                return -1;
            }

            var result = _qustionnaireService.Set(customer.SiteId, customer.CustId, customer.CurrencyId, request.Content, request.ContactInfo);

            if(result == 1) 
            {
                _redisHelper.StringSet(key, cnt + 1, TimeSpan.FromDays(1), RedisDataBase.Shared);
            }

            return result;
        }
    }
}