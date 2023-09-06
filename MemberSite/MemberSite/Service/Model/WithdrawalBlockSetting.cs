using MemberSite.Service.Model.enums;

namespace MemberSite.Service.Model;

public class WithdrawalBlockSetting
{
    public int CurrencyId { get; set; }
    public WithdrawalBlockTypeEnum Type { get; set; }
    public bool Status { get; set; }
}
