using MemberSite.Service.DAL;
using MemberSite.Service.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }
}
