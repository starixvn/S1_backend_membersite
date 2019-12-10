using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemberSite.Service.BLL;
using MemberSite.Service.Library;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MemberSite.Controllers
{
    public class entPageController : Controller
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        public entPageController(ThirdPartyBLL thirdPartyBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
        }
        public class TpInfo
        {
            public int TpId { set; get; }
            public Dictionary<string, string> inputs { set; get; }
        }

        [HttpGet]
        public IActionResult Index(int tpId, Dictionary<string, string> inputs)
        {
            if (User.Identity.IsAuthenticated)
            {
                return View();
            }
            else if (hasBf(tpId))
            {
                string entryUrl = "~/";

                entryUrl = thirdPartyBLL.getEntryUrl(tpId, inputs);

                return Redirect(entryUrl);
            }
            else
            {
                return StatusCode(400);
            }
        }

        Dictionary<int, bool> Bf = new Dictionary<int, bool>()
        {
           {1,false }
        };


        private bool hasBf(int tpId)
        {
            return Bf.ContainsKey(tpId);
        }
    }
}