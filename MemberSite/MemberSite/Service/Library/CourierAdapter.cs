using MemberSite.Service.Model;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Serilog.Context;

namespace MemberSite.Service.Library
{
    public class CourierAdapter
    {
        private static readonly Lazy<HttpClient> lazy = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        });

        private HttpClient client => lazy.Value;
        private readonly ILogger<CourierAdapter> _logger;

        public CourierAdapter(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CourierAdapter>>();
        }

        public async Task<ApiResult<T>> MyPostAsync<T, U>(string url, U oTpInfo)
        {
            ApiResult<T> oApiResult;
            var fooJson = JsonConvert.SerializeObject(oTpInfo);
            LogContext.PushProperty("TpInfo", fooJson);
            LogContext.PushProperty("Tp.Request.URL", url);
            var fooContent = new StringContent(fooJson, Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync(url, fooContent);
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Tp.Request.Response:{result}");

                if (response.IsSuccessStatusCode)
                {
                    oApiResult = JsonConvert.DeserializeObject<ApiResult<T>>(result);
                }
                else
                {
                    oApiResult = new ApiResult<T>
                    {
                        ErrorCode = 9000,
                        RawMessage = $"{result} {url}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                oApiResult = new ApiResult<T>
                {
                    ErrorCode = 9000,
                    RawMessage = $"{ex.Message} {url}"
                };
            }
            return oApiResult;
        }
        
        public async Task<T> ShieldGetAsync<T>(string url, ICollection<KeyValuePair<string, string>> header) where T : new()
        {
            T oApiResult = default;
            LogContext.PushProperty("Shield.Header", JsonConvert.SerializeObject(header));
            LogContext.PushProperty("Shield.Request.URL", url);

            using var httpClient = new HttpClient();
            
            foreach (var (key, value) in header)
            {
                httpClient.DefaultRequestHeaders.Add(key, value);
            }
            
            try
            {
                using var response = await httpClient.GetAsync(url);
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Shield.Response:{result}");

                if (response.IsSuccessStatusCode)
                {
                    oApiResult = JsonConvert.DeserializeObject<T>(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return oApiResult;
        }
        
        public async Task<T> ShieldPostAsync<T, U>(string url, U request , ICollection<KeyValuePair<string, string>> header) where T : new()
        {
            T oApiResult = default;
            var reqJson = JsonConvert.SerializeObject(request);
            var reqContent = new StringContent(reqJson, Encoding.UTF8, "application/json");
            
            LogContext.PushProperty("Shield.Header", JsonConvert.SerializeObject(header));
            LogContext.PushProperty("Shield.Request.URL", url);
            _logger.LogInformation($"Shield.Request:{reqJson}");
            
            using var httpClient = new HttpClient();
            foreach (var (key, value) in header)
            {
                httpClient.DefaultRequestHeaders.Add(key, value);
            }
            try
            {
                using var response = await httpClient.PostAsync(url, reqContent);
                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Shield.Response:{result}");

                if (response.IsSuccessStatusCode)
                {
                    oApiResult = JsonConvert.DeserializeObject<T>(result);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return oApiResult;
		}
		
        public async Task<object> PostAsync<TRequest>(string url, TRequest request, int timeout = 0)
        {
            try
            {
                object result = default;
                var jsonReq = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonReq, Encoding.UTF8, "application/json");
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (timeout != 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(timeout);
                }

                _logger.LogInformation($"url:{url}, request:{jsonReq}");
                using var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"response:{responseBody}");
                    result = System.Text.Json.JsonSerializer.Deserialize<object>(responseBody); // 序列化走System.Text.Json
                }

                return result;

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
