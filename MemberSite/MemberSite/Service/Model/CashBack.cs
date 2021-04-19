using System;

namespace MemberSite.Service.Model
{
    public class CustCashBackInput
    {
        public CustCashBackInput(int custId, string bonusCode, int categoryId, int promoCategoryId,
            string calculateDate, decimal bonusAmount, DateTime? claimTime)
        {
            CustId = custId;
            BonusCode = bonusCode;
            CategoryId = categoryId;
            PromoCategoryId = promoCategoryId;
            CalculateDate = calculateDate;
            BonusAmount = bonusAmount;
            ClaimTime = claimTime;
        }

        public int CustId { get; }
        public string BonusCode { get; }
        public int CategoryId { get; }
        public int PromoCategoryId { get; }
        public string CalculateDate { get; }
        public decimal BonusAmount { get; }
        public DateTime? ClaimTime { get; }
    }

    public class CustCashBackCheckOutput
    {
        public bool Claimed { get; set; }
    }
}