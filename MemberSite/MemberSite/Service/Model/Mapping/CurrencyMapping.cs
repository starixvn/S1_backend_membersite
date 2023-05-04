namespace MemberSite.Service.Model.Mapping
{
    public class CurrencyMapping
    {
        public Dictionary<int, string> CountryMap { get; }
        
        public CurrencyMapping()
        {
            CountryMap = new Dictionary<int, string>
            {
                {2,"MY" },
                {3,"US" },
                {4,"TH" },
                {13,"CS" },
                {15,"ID" },
                {51,"VN" },
                {61,"IN" },
                {72,"KH" },
                {74,"BD" },
                {96,"MY" }
            };
        }
    }

    public class AreaCodeMap
    {
        public string AreaCode { get; set; }
        public string Code { get; set; }
    }
}