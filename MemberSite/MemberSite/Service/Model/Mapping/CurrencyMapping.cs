using System.Collections.Generic;

namespace MemberSite.Service.Model.Mapping
{
    public class CurrencyMapping
    {
        public static Dictionary<int, string> CountryMap;

        public CurrencyMapping()
        {
            CountryMap = new Dictionary<int, string>
            {
                {2,"MY" },{4,"TH" },{13,"CS" },{15,"ID" },{51,"VN" },{61,"IN" },{96,"MY" }
            };
        }
        
    }
}