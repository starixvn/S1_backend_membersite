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
                case "Staging":
                    return "S";
                case "Production":
                    return "P";
                case "QA":
                    return "Q";
                default:
                    return "P";
            }
        }
    }
}
