using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.BLL
{
    public class WalletBLL
    {
        private readonly Tools tools;
        private readonly WalletService walletService;

        public WalletBLL(Tools tools, WalletService walletService)
        {
            this.tools = tools;
            this.walletService = walletService;
        }
        public decimal GetBalance(int custId)
        {
            return walletService.GetBalance(custId);
        }
        public string GetFmtBalance(int custId)
        {
            return tools.FmtBalance(walletService.GetBalance(custId));
        }
    }
}
