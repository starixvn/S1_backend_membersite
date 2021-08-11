namespace MemberSite.Service.Model
{
    public class CustCashBackInput
    {
        public CustCashBackInput(int custId, string bonusCode, int promoCategoryId, string calculateDate)
        {
            CustId = custId;
            BonusCode = bonusCode;
            PromoCategoryId = promoCategoryId;
            CalculateDate = calculateDate;
        }

        public int CustId { get; }
        public string BonusCode { get; }
        public int PromoCategoryId { get; }
        public string CalculateDate { get; }
    }

    public class CustCashBackCheckOutput
    {
        public bool Claimed { get; set; }
    }
}