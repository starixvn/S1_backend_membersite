namespace MemberSite.Extensions
{ 
    public static class EnumerableExtensions
    {
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                var elementValue = keySelector(element);
                if (seenKeys.Add(elementValue))
                {
                    yield return element;
                }
            }
        }

        public static T GetRandom<T>(this IEnumerable<T> source)
        {
            var index = new Random().Next(0, source.Count());
            return source.ElementAt(index);
        }
    }
}
