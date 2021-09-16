using MemberSite.Service.DAL;

namespace MemberSite.Service.BLL
{
    public class AuthBLL
    {
        private readonly AuthService authService;

        public AuthBLL(AuthService authService)
        {
            this.authService = authService;
        }

        public bool Authenticate<T>(string sToken, out T t)
        {
            var sPermissionData = "";
            t = default;
            sPermissionData = authService.Authenticate(sToken);

            if (!string.IsNullOrEmpty(sPermissionData))
            {
                t = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(sPermissionData);
                return true;
            }

            return false;
        }
    }
}
