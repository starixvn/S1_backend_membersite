using System.Text;

namespace MemberSite.Extensions;

public static class CommonExtension
{
    public static void AddRange<T, S>(this IDictionary<T, S> source, IDictionary<T, S> collection)
    {
        foreach (var (key, value) in collection)
        {
            source.Add(key, value);
        }
    }

    public static void AddRange<T>(this IEnumerable<T> collection, IEnumerable<T> items)
    {
        items?.Aggregate(collection, (current, obj) => current.Append(obj));
    }

    public static string GenerateRandomNumber(int length = 6)
    {
        var random = new Random();
        var sb = new StringBuilder();

        for (var i = 0; i < length; i++)
        {
            sb.Append(random.Next(0, 10));
        }

        return sb.ToString();
    }
}