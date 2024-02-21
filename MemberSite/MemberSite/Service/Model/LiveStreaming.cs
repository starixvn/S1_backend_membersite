using MemberSite.Service.Model.enums;
using Newtonsoft.Json;

namespace MemberSite.Service.Model
{
    public class LiveStreaming
    {
        public string MatchID { get; set; }
        public int Channel { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime TimeStop { get; set; }
        public string Name { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }
        public string HomeCH { get; set; }
        public string AwayCH { get; set; }
        public string HomeTH { get; set; }
        public string AwayTH { get; set; }
        public string Type { get; set; }
        public string League { get; set; }
        public string LeagueCH { get; set; }
        public string LeagueTH { get; set; }
        public bool NowPlaying { get; set; }
        public bool IsLive { get; set; }
        public string State { get; set; }
        public long? Bid { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
    }

    public class LiveStreamingData
    {
        public string MatchID { get; set; }
        public int Channel { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime TimeStop { get; set; }
        public string Name { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }
        public string Type { get; set; }
        public string League { get; set; }
        public bool NowPlaying { get; set; }
        public bool IsLive { get; set; }
        public string State { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
    }
    public class LiveStreamingLinkReq
    {
        public int Ch { set; get; }
        public string SiteName { set; get; }
        public string Uid { set; get; }
        public string Ip { set; get; }
    }

    public class SportLiveStreamingResponse : HotEventResponse
    {

    }
    public class SportLiveStreamingData : HotEventDto
    {
        private string Currency { get; set; }
        public IEnumerable<int> ICurrencyList
        {
            get
            {
                return string.IsNullOrWhiteSpace(Currency) ? 
                    null:
                    JsonConvert.DeserializeObject<IEnumerable<int>>(Currency);
            }
        }
    }
}
