using MemberSite.Service.Model;

using Newtonsoft.Json;

namespace MemberSite.Service.BLL
{
    public class ConfigBLL
    {
        private readonly Dictionary<string, string> _defaultSiteConfig;
        private readonly Dictionary<string, SiteConfigObject> _siteNameConfigs;
        private readonly Dictionary<int, SiteConfigObject> _siteIdConfigs;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ConfigBLL(IServiceProvider provider)
        {
            var siteConfigList = provider.GetServices<SiteConfigObject>();
            _defaultSiteConfig = siteConfigList.First(i => i.SiteId == 0).Config;

            var siteConfigs = siteConfigList.Where(i => i.SiteId != 0);
            _siteNameConfigs = new Dictionary<string, SiteConfigObject>(siteConfigs.ToDictionary(i => i.SiteName, i => i), StringComparer.OrdinalIgnoreCase);
            _siteIdConfigs = siteConfigs.ToDictionary(i => i.SiteId, i => i);
            _environment = provider.GetRequiredService<IWebHostEnvironment>();
            _configuration = provider.GetRequiredService<IConfiguration>();
        }

        public string GetConfigValue(string siteName, string configKey)
        {
            var defaultValue = _defaultSiteConfig.GetValueOrDefault(configKey, string.Empty);
            if (_siteNameConfigs.TryGetValue(siteName, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : defaultValue;
            }

            return defaultValue;
        }

        public string GetConfigValue(string configKey, int siteId = 0)
        {
            var defaultValue = _defaultSiteConfig.GetValueOrDefault(configKey, string.Empty);
            if (_siteIdConfigs.TryGetValue(siteId, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue) ? configValue : defaultValue;
            }

            return defaultValue;
        }

        public TItem GetConfigValue<TItem>(string siteName, string configKey)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
                ? JsonConvert.DeserializeObject<TItem>(json) : default;

            if (_siteNameConfigs.TryGetValue(siteName, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue)
                    ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
            }

            return defaultItem;
        }

        public TItem GetConfigValue<TItem>(string configKey, int siteId = 0)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
                ? JsonConvert.DeserializeObject<TItem>(json) : default;

            if (_siteIdConfigs.TryGetValue(siteId, out var siteConfig))
            {
                return siteConfig.Config.TryGetValue(configKey, out var configValue)
                   ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
            }

            return defaultItem;
        }

        public Dictionary<int, TItem> GetConfigValuesBySiteId<TItem>(string configKey)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
                ? JsonConvert.DeserializeObject<TItem>(json) : default;

            var result = new Dictionary<int, TItem>();
            foreach (var siteConfig in _siteIdConfigs)
            {
                var data = siteConfig.Value.Config.TryGetValue(configKey, out var configValue)
                    ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
                result.Add(siteConfig.Key, data);
            }
            return result;
        }

        public Dictionary<string, TItem> GetConfigValuesBySiteName<TItem>(string configKey)
        {
            var defaultItem = _defaultSiteConfig.TryGetValue(configKey, out var json)
               ? JsonConvert.DeserializeObject<TItem>(json) : default;

            var result = new Dictionary<string, TItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var siteConfig in _siteNameConfigs)
            {
                var data = siteConfig.Value.Config.TryGetValue(configKey, out var configValue)
                   ? JsonConvert.DeserializeObject<TItem>(configValue) : defaultItem;
                result.Add(siteConfig.Key, data);
            }
            return result;
        }

        public TItem GetConfigValueByEnvironment<TItem>(string configKey, int siteId = 0)
        {
            var environmentName = _configuration.GetSection("Environment").Value;
            var json = GetConfigValue($"{configKey}.{environmentName}.json", siteId);
            return !string.IsNullOrEmpty(json) ? JsonConvert.DeserializeObject<TItem>(json) : default;
        }
    }
}
