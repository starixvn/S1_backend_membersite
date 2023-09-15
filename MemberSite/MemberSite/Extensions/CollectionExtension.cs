using System.Reflection;
using MemberSite.Attributes;

namespace MemberSite.Extensions;

public static class CollectionExtension
{
    public static Dictionary<string, string> ClassToDic<T>(this T obj)
    {
        return obj.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => !x.GetCustomAttributes(typeof(DicIgnoreAttribute)).Any())
            .ToDictionary(prop => prop.Name, prop => prop.GetValue(obj)?.ToString());
    } 
}