using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
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
        private readonly ServerInfoService serverInfoService;

        public WalletBLL(Tools tools, WalletService walletService, ServerInfoService serverInfoService)
        {
            this.tools = tools;
            this.walletService = walletService;
            this.serverInfoService = serverInfoService;
        }
        public decimal GetBalance(int custId)
        {
            return walletService.GetBalance(custId);
        }
        public string GetFmtBalance(int custId)
        {
            return tools.FmtBalance(walletService.GetBalance(custId));
        }

        public WalletInfo GetWalletInfo(int custId)
        {
            var serverInfo = serverInfoService.GetModel();
            return walletService.GetWalletInfo(custId, serverInfo.SiteId);
        }
    }
}
