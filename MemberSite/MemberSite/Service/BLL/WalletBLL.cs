using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class WalletBLL
    {
        private readonly Tools tools;
        private readonly WalletService walletService;
        private readonly ServerInfoBll serverInfoBLL;

        public WalletBLL(Tools tools, WalletService walletService, ServerInfoBll serverInfoBLL)
        {
            this.tools = tools;
            this.walletService = walletService;
            this.serverInfoBLL = serverInfoBLL;
        }
        public decimal GetBalance(int custId)
        {
            return tools.Floor(walletService.GetBalance(custId), 2);
        }
        public decimal GetBonus(int custId, int promoCategoryId)
        {
            return tools.Floor(walletService.GetBonus(custId, promoCategoryId), 2);
        }

        public WalletInfo GetWalletInfo(int custId)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            return walletService.GetWalletInfo(custId, serverInfo.SiteId);
        }
        public int GetWalletTransferStatus(int custId, out int autoTransfer)
        {
            return walletService.GetWalletTransferStatus(custId, out autoTransfer);
        }

        public int SetWalletTransferStatus(int custId, int autoTransfer)
        {
            var serverInfo = serverInfoBLL.GetServerInfo();
            return walletService.SetWalletTransferStatus(custId, autoTransfer);
        }
    }
}
