using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class Provider
    {
        public short Value { get; set; }
        public string Name { get; set; }
        public string DisplayText { get; set; }
        public bool PlayFree { get; set; }
        public GameDesc GameList { get; set; }
    }

    public class GameDesc
    {
        public string ImgUrl { get; set; }

        public IEnumerable<Datum> Data { get; set; }
    }

    public partial class Datum
    {
        public Dictionary<string, string> Name { get; set; }
        public string AppId { get; set; }
        public string GameId { get; set; }
        public string Category1 { get; set; }
        public string Paylines { get; set; }
        public string Rtp { get; set; }
        public string Reels { get; set; }
        public Dictionary<string, string> ImgName { get; set; }
        public string LaunchDate { get; set; }
        public bool Hot { get; set; }
        public bool New { get; set; }
        public bool ImageExist { get; set; }
    }
}
