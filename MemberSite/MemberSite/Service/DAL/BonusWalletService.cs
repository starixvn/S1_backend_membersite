using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class BonusWalletService
    {
        private readonly MySqlClient _reportDbClient;

        public BonusWalletService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<BonusWallet> GetCustBonusWallet(int CustId)
        {
            var datas = _reportDbClient.Query<BonusWallet>("Member_CustBonus_Get_V2", new { CustId });

            foreach (var data in datas)
            {
                data.Turnover = data.Turnover < 0 ? 0 : data.Turnover;
            }

            return datas;
        }

        public BonusWallet CheckBonusWallet(int CustId, int TpId)
        {
            return _reportDbClient.QueryFirst<BonusWallet>("Member_CustBonus_Check", new { CustId, TpId });
        }
        public BonusWallet CheckBonusWallet(int CustId, string bonusCode)
        {
            return _reportDbClient.QueryFirst<BonusWallet>("Member_CustBonusByCode_Get", new { CustId, BonusCode = bonusCode });
        }

        public BonusWallet CustBonusInfo(int CustId, string BonusCode, int TpId, int PromoCategoryId)
        {
            return _reportDbClient.QueryFirst<BonusWallet>("Member_CustBonus_GetById", new { CustId, BonusCode, TpId, PromoCategoryId });
        }
    }
}
