namespace MemberSite.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsBetween(this DateTime dt, DateTime startDate, DateTime endDate, bool compareTime = true)
        {
            return compareTime ?
               dt >= startDate && dt <= endDate :
               dt.Date >= startDate.Date && dt.Date <= endDate.Date;
        }

        /// <param name="timeZone">format:+08:00</param>
        public static DateTimeOffset ToDateTimeOffset(this DateTime time, string timeZone)
        {
            return DateTimeOffset.Parse(time.ToString() + timeZone);
        }

        public static IEnumerable<string> GetMonthlyPartitionRange(this DateTime startDate, DateTime endDate)
        {
            var result = new List<string>();
            while (startDate.EarlierThan(endDate))
            {
                result.Add(startDate.GetMonthlyPartitionKey());
                startDate = startDate.AddMonths(1);
            }
            return result;
        }

        public static string GetMonthlyPartitionKey(this DateTime partitionTime)
        {
            return partitionTime.ToString("yyyyMM");
        }

        public static bool EarlierThan(this DateTime startMonth, DateTime endMonth)
        {
            var start = int.Parse(startMonth.ToString("yyyyMM"));
            var end = int.Parse(endMonth.ToString("yyyyMM"));
            return start <= end;
        }
    }
}
