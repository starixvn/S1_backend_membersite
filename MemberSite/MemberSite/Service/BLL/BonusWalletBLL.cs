using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using System.Collections.Generic;

namespace MemberSite.Service.BLL
{
    public class BonusWalletBLL
    {
        private readonly BonusWalletService bonusWalletService;

        public BonusWalletBLL(BonusWalletService bonusWalletService)
        {
            this.bonusWalletService = bonusWalletService;
        }

        public IEnumerable<BonusWallet> GetCustBonusWallet(int CustId)
        {
            return bonusWalletService.GetCustBonusWallet(CustId);
        }

        public BonusWallet CheckBonusWallet(int CustId, int TpId)
        {
            return bonusWalletService.CheckBonusWallet(CustId, TpId);
        }
        public BonusWallet CheckBonusWallet(int CustId, string bonusCode)
        {
            return bonusWalletService.CheckBonusWallet(CustId, bonusCode);
        }
        public BonusWallet CustBonusInfo(int CustId, string BonusCode)
        {
            return bonusWalletService.CustBonusInfo(CustId, BonusCode);
        }

        public BonusWallet GetCustSpecificBonus(int CustId, string BonusCode, int TpId, int PromoCategoryId)
        {
            return bonusWalletService.CustBonusInfo(CustId, BonusCode, TpId, PromoCategoryId);
        }
    }
}
