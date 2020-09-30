using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.Model
{
    public class SendOTPSMSResult
    {
        public int Status { get; set; }
        public long StartUnixTime { get; set; }
    }

}
