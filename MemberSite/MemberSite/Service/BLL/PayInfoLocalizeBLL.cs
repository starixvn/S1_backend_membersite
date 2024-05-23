using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL;

public class PayInfoLocalizeBLL
{
    
    private readonly AuthorizeBLL _authorizeBll;
    private readonly ICacheHelper _redisHelper;
    private readonly PayService _payService;
    
    public PayInfoLocalizeBLL(IServiceProvider provider)
    {
        _payService = provider.GetRequiredService<PayService>();
        _authorizeBll = provider.GetRequiredService<AuthorizeBLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
    }
    
    internal IEnumerable<T> PayGroupNameLocalization<T>(IEnumerable<T> pgList, string lang) where T : class, IPayGroupName
    {
        var pgNames = GetPayGroupNameWithCache()
            .Where(x => x.Lang.Equals(lang, StringComparison.OrdinalIgnoreCase));

        if (!pgNames.Any())
            return pgList;

        foreach (var pg in pgList)
        {
            pg.DisplayGroupName = pgNames
                .FirstOrDefault(x => x.GroupId == pg.GroupId)?.GroupName ?? pg.GroupName;
        }
            
        return pgList;
    }
        
    internal IEnumerable<T> PayChannelNameLocalization<T>(IEnumerable<T> pcList, string lang) where T : class, IPayChannelName
    {
        var pcNames = GetPayChannelNameWithCache()
            .Where(x => x.Lang.Equals(lang, StringComparison.OrdinalIgnoreCase));

        if (!pcNames.Any())
            return pcList;

        foreach (var pc in pcList)
        {
            pc.DisplayChannelName = pcNames
                .FirstOrDefault(x => x.ChannelId == pc.ChannelId)?.ChannelName ?? pc.ChannelName;
        }
            
        return pcList;
    }
    
    internal IEnumerable<PayGroupName> GetPayGroupNameWithCache()
    {
        var authData = _authorizeBll.GetAuthData();
        var lang = authData.LangId;
            
        var dbData = _redisHelper.TryGetOrCreate("PayGroupNameList", TimeSpan.FromMinutes(5),
            () => _payService.GetPayGroupName());
        return dbData;
    }

    internal IEnumerable<PayChannelName> GetPayChannelNameWithCache()
    {
        var dbData = _redisHelper.TryGetOrCreate("PayChannelNameList", TimeSpan.FromMinutes(5),
            () => _payService.GetPayChannelName());
        return dbData;
    }

}