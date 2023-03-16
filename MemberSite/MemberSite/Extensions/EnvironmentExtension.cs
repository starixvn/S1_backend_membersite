namespace MemberSite.Extensions
{
    public static class EnvironmentExtension
    {
        public static bool IsPrd(this IHostEnvironment hostEnvironment)
        {
            if (hostEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostEnvironment));
            }

            return hostEnvironment.IsProduction() || hostEnvironment.EnvironmentName == "Release" || hostEnvironment.EnvironmentName == "Production.APP";
        }
    }
}
