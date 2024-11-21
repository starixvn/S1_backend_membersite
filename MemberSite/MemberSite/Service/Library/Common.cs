using System;

namespace MemberSite.Service.Library
{
    public class Common
    {
        private readonly IWebHostEnvironment _environment;

        public Common(IWebHostEnvironment environment)
        {
            this._environment = environment;
        }

        public string GetEnv()
        {
            switch (_environment.EnvironmentName)
            {
                case "Development":
                case "LOCAL":
                    return "D";

                case "SIT":
                    return "S";

                case "UAT":
                    return "U";

                case "PROD":
                    return "P";

                default:
                    return "D";
            }
        }
    }
};
