using System.ComponentModel;

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
}