using MemberSite.Service.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberSite.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly TemplateBLL templateBLL;

        public AccountController(TemplateBLL templateBLL)
        {
            this.templateBLL = templateBLL;
        }

        public IActionResult Profile()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
        public IActionResult ChangePwd()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
        public IActionResult CustBankInfo()
        {
            return View(templateBLL.GetTemplatePath(Url));
        }
        public IActionResult Messages()
        {
            return View();
        }
        public IActionResult ResetPwd()
        {
           return View(templateBLL.GetTemplatePath(Url));
        }
    }
}