using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class DateTimeHelper
    {
        private readonly CurrencyService _currencyService;
        private readonly ICacheHelper _redisHelper;

        public DateTimeHelper(ICacheHelper redisHelper, CurrencyService currencyService)
        {
            _currencyService = currencyService;
            _redisHelper = redisHelper;
        }

        public async Task<DateTimeOffset> ToCurrencyZone(DateTime time, int currencyId)
        {
            var timezone = await _redisHelper.TryGetOrCreateAsync($"TimeZone:{currencyId}", null,
                async () => await _currencyService.GetTimeZone(currencyId), RedisDataBase.Shared);

            return new DateTimeOffset(new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second), TimeSpan.Zero).ToOffset(TimeSpan.Parse(timezone.TrimStart('+')));
        }
    }
}
