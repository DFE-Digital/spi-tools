namespace Dfe.Spi.LocalPreparer.Common.Utils;
public static class MemoryExtensions
{

    public static IEnumerable<ReadOnlyMemory<T>> SplitIntoChunks<T>(this ReadOnlyMemory<T> sourceList, int chunkSize)
    {
        var numChunks = (int)Math.Ceiling((double)sourceList.Length / chunkSize);

        for (var i = 0; i < numChunks; i++)
        {
            var startIndex = i * chunkSize;
            var endIndex = startIndex + chunkSize;

            if (endIndex > sourceList.Length)
            {
                endIndex = sourceList.Length;
            }

            var chunk = sourceList.Slice(startIndex, endIndex - startIndex);

            yield return chunk;
        }
    }

}
