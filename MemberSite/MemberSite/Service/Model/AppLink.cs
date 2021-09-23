using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class AppLink
    {
        public int AppType { get; set; }
        public int SiteId { get; set; }
        public string Domain { get; set; }
        public int ProtocolType { get; set; }
    }
    public class GetAppLinkRep
    {
        public int AppType { get; set; }
        public string AppTypeName { get; set; }
        public string DownloadLink { get; set; }

    }
}
