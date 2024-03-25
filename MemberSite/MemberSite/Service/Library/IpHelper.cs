using System.Net;

using MemberSite.Service.BLL;

using Microsoft.AspNetCore.HttpOverrides;

namespace MemberSite.Service.Library;

public class IpHelper
{
    private readonly List<string> _allowIpList;

    public IpHelper(IServiceProvider provider)
    {
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _allowIpList = configBll.GetConfigValue<string[]>("IpRange").ToList();
    }

    public bool IpInRange(string ip)
    {
        var incomingIp = IPAddress.Parse(ip);
        foreach (var subnet in _allowIpList)
        {
            var parts = subnet.Split('/');
            var ipRange = IPAddress.Parse(parts[0]);
            var mask = Convert.ToInt32(parts[1]);
            var network = new IPNetwork(ipRange, mask);
            if (network.Contains(incomingIp))
                return true;
        }
        return false;
    }
}