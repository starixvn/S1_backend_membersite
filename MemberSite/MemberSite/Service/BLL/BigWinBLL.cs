using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class BigWinBLL
    {
        private readonly ICacheHelper _redisHelper;
        private readonly BigWinService _bigWinService;
        private readonly SlotGameBLL _slotGameBLL;
        private readonly GameBLL _gameBll;

        public BigWinBLL(IServiceProvider provider, ICacheHelper redisHelper)
        {
            this._redisHelper = redisHelper;
            _bigWinService = provider.GetRequiredService<BigWinService>();
            _slotGameBLL = provider.GetRequiredService<SlotGameBLL>();
            _gameBll = provider.GetRequiredService<GameBLL>();
        }

        public async Task<BigWinPopUp> GetBigWinPopUpData(int siteId, int currencyId, string langId, int deviceType)
        {
            var result = new BigWinPopUp();
            var key = $"GetBigWinPopUpDataList:{siteId}:{currencyId}";
            var bigWinPopUpDataList = await _redisHelper.TryGetOrCreateAsync(key, TimeSpan.FromMinutes(15),
                async () => await _bigWinService.GetBigWinPopUpDataList(siteId, currencyId));

            if (bigWinPopUpDataList.Any() && bigWinPopUpDataList.FirstOrDefault().Time == DateTime.UtcNow.Date.AddDays(-1))
            {
                var bigWinPopUpData = bigWinPopUpDataList.FirstOrDefault();
                result.TpId = bigWinPopUpData.TpId;
                result.TpCategory = bigWinPopUpData.TpCategory;
                result.TpGameType = bigWinPopUpData.TpGameType;
                result.GameId = bigWinPopUpData.GameID;
                result.Amount = bigWinPopUpData.WinlostAmount;
                result.UserName = MaskUserName(bigWinPopUpData.MemberID);

                if (bigWinPopUpData.TpCategory == (int)TpCategory.Casino)
                {
                    // 取出Slot的遊戲資訊
                    var slotGameList = await _slotGameBLL.GetSlotGameList(siteId, currencyId, deviceType);

                    var gameSlotDetail = slotGameList.FirstOrDefault(i => i.TpId == bigWinPopUpData.TpId && (i.GameId == bigWinPopUpData.GameID || i.AppId == bigWinPopUpData.GameID));

                    if (gameSlotDetail != null)
                    {
                        result.GameName = gameSlotDetail.GameName.ContainsKey("en") ? gameSlotDetail.GameName["en"] : string.Empty;
                    }
                }
                else
                {
                    // 取出其他大廳的遊戲資訊
                    var tpGameIdList = await _gameBll.GetTpGameIdList();

                    result.GameName = tpGameIdList.FirstOrDefault(r => r.TpId == bigWinPopUpData.TpId)?.TypeName;
                }

            }

            return result;
        }

        private string MaskUserName(string username)
        {
            string maskstr, maskchar;
            maskchar = null;
            if (!string.IsNullOrEmpty(username))
            {
                int End = (int)(username.Length - 4);
                maskstr = username.Substring(4, End);
                for (int i = 0; i < maskstr.Length; i++)
                {
                    maskchar = maskchar + "*";
                }
                username = username.Replace(maskstr, maskchar);

            }
            else
            {

                username = "****";
            }
            return username;


        }
    }
}
