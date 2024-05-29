using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model;


public class DepositReqParams
{
    public int CustId { get; set; }
    public DepositType DepositType { get; set; }
    public IDictionary<string, string> ExtraData { get; set; }
    public int ChannelId { get; set; }
    public decimal Amount { get; set; }
}