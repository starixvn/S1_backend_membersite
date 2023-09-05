using System.Data;

using Dapper;

using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.DAL
{
    public class WalletService
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public WalletService(IServiceProvider serviceProvider)
        {
            _configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public int SetBalance(int custId, int siteId, string userName, decimal amount, int type, string memo, string refTransId, out decimal afterAmount, out byte is1stDepoist, IDbTransaction trans)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId,
                SiteId = siteId,
                UserName = userName,
                Amount = amount,
                Type = type,
                Memo = memo,
                RefTransId = refTransId
            });

            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Wallet_Set_V2", trans, p);
            is1stDepoist = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal?>("AfterAmount") ?? 0;

            return afterAmount >= 0 ? 1 : 0;
        }

        public decimal GetBalance(int custId)
        {
            return _reportDbClient.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId });
        }

        public decimal GetBonus(int custId, int promoCategoryId)
        {
            return _reportDbClient.QueryFirst<decimal>("Service_Bonus_Get", new { CustId = custId, PromoCategoryId = promoCategoryId });
        }

        public WalletInfo GetWalletInfo(int custId, int siteId)
        {
            var walletInfo = _reportDbClient.QueryFirst<WalletInfo>("Service_CustInfoLog_Get", new { CustId = custId, SiteId = siteId });
            walletInfo.Balance = GetBalance(custId);

            return walletInfo;
        }

        public int GetWalletTransferStatus(int custId, out int autoTransfer)
        {
            autoTransfer = 0;
            var p = new DynamicParameters(new { CustId = custId });
            p.Add("AutoTransfer", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var result = _reportDbClient.Execute("Member_CustWalletTransferStatus_Get", p);
            autoTransfer = p.Get<int?>("AutoTransfer") ?? 0;

            return result >= 0 ? 1 : 0;
        }

        public int SetWalletTransferStatus(int custId, int autoTransfer)
        {
            return _mainDbClient.Execute("Member_CustWalletTransferStatus_Set", new { CustId = custId, AutoTransfer = autoTransfer }) > 0 ? 1 : 0;
        }
    }
}
