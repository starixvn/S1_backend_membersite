namespace MemberSite.Extensions
{
    public static class StringExtension
    {
        public static bool IsJson(this string value)
        {
            return (value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("[") && value.EndsWith("]"));
        }
    }
}
