namespace Dfe.Spi.LocalPreparer.Common.Utils;
public static class LinqExtensions
{
    public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> sourceList, int ListSize)
    {
        while (sourceList.Any())
        {
            yield return sourceList.Take(ListSize);
            sourceList = sourceList.Skip(ListSize);
        }
    }
}
