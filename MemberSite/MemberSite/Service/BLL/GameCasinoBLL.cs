using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class GameCasinoBLL
    {
        private readonly GameCasinoService gameCasinoService;
        private readonly IMemoryCache cache;
        private readonly StatementService statementService;

        public GameCasinoBLL(IMemoryCache cache,GameCasinoService gameCasinoService, StatementService statementService)
        {
            this.cache = cache;
            this.gameCasinoService = gameCasinoService;
            this.statementService = statementService;
        }
        public List<GameCasinoListData> GetGameCasinoList()
        {
            var key = $"GameCasinoList";
            var gameCasinoListData = cache.Get<List<GameCasinoListData>>(key);

            if(gameCasinoListData != null)
            {
                return gameCasinoListData;
            }

            gameCasinoListData = gameCasinoService.GetGameCasinoList();

            if(gameCasinoListData != null)
            {
                cache.Set(key, gameCasinoListData, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)));
            }
            return gameCasinoListData;
        }
        public List<GameCasinoListData> GetGameCasinoListByDevice(int deviceType)
        {
            var key = $"GetGameCasinoListByDevice_{deviceType}";
            var gameCasinoListData = cache.Get<List<GameCasinoListData>>(key);

            if (gameCasinoListData != null)
            {
                return gameCasinoListData;
            }

            gameCasinoListData = gameCasinoService.GetGameCasinoListByDevice(deviceType);

            if (gameCasinoListData != null)
            {
                cache.Set(key, gameCasinoListData, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)));
            }
            return gameCasinoListData;
        }
        public List<Provider> GetProviderList()
        {
            var Provider = new List<Provider>();
            var tpList = statementService.GetTpListWithCache().ToList();
            tpList.ForEach(x =>
            {
                Provider.Add(new Provider {
                    Value = Convert.ToInt16(x.Id),
                    Name = x.TpName,
                    PlayFree = x.Fun,
                    DisplayText = x.TpName
                });
            });
            return Provider;
        }
        public Dictionary<string, string> OrgFileNameFilter(Dictionary<string, string> ImgNameDictionary)
        {
            var newImgNameDictionary = new Dictionary<string, string>();
            foreach (var ImgName in ImgNameDictionary)
            {
                newImgNameDictionary.Add(ImgName.Key, ImgName.Value.Replace(" ", "-").Replace("&", "-").Replace(":", "-").Replace("'", "").Replace("’", "").Replace(".", "").Replace("™", "").Replace("---", "-").Replace("--", "-").ToLower()
                    + ".jpg");
            }
            return newImgNameDictionary;
        }
    }
}
