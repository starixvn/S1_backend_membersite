using System.Net;
using MemberSite.Service.Model;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace MemberSite.Service.Library;

public class IpHelper
{
    private readonly IOptions<AllowIpList> _allowIpList;

    public IpHelper(IServiceProvider provider)
    {
        _allowIpList = provider.GetRequiredService<IOptions<AllowIpList>>();
    }

    public bool IpInRange(string ip)
    {
        var incomingIp = IPAddress.Parse(ip);
        foreach (var subnet in _allowIpList.Value.IpRange)
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