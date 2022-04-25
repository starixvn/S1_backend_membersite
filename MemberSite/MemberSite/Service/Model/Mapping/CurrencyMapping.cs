namespace MemberSite.Service.Model.Mapping
{
    public class CurrencyMapping
    {
        public Dictionary<int, string> CountryMap { get; }
        public Dictionary<string, string> AreaCodeMap { get; }

        public CurrencyMapping()
        {
            CountryMap = new Dictionary<int, string>
            {
                {2,"MY" },{4,"TH" },{13,"CS" },{15,"ID" },{51,"VN" },{61,"IN" },{96,"MY" }
            };
            
            AreaCodeMap = new Dictionary<string, string>()
            {
                {"+60","MY" },
                {"+66","TH" },
                {"+86","CS" },
                {"+62","ID" },
                {"+91","IN" },
                {"+84","VN" }
            };
        }
    }
}