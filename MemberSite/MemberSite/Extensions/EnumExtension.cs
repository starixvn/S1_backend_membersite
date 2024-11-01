using System.ComponentModel;
using MemberSite.Service.Model;

namespace MemberSite.Extensions;

public static class EnumExtension
{
    /// <summary>
    /// 取得列舉說明文字
    /// </summary>
    /// <param name="value">列舉值</param>
    /// <returns></returns>
    public static string GetDescription(this Enum value)
    {
        var fi = value.GetType().GetField(value.ToString());
        var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
            typeof(DescriptionAttribute), false);

        return attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }
    
    public static IEnumerable<SelectListItem> EnumToSelectListItem<T>(IEnumerable<T> except = null)
        where T : Enum
    {
        return EnumToIEnumerable(except)
            .Select(x => new SelectListItem
            {
                Value = Convert.ToInt32(x),
                Name = x.GetDescription()
            });
    }
    
    public static IEnumerable<T> EnumToIEnumerable<T>(IEnumerable<T> except = null)
        where T : Enum
    {
        var type = typeof(T);
        return Enum.GetValues(type)
            .Cast<T>()
            .Where(x => except == null || !except.Contains(x));
    }
}