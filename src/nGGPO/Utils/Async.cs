using nGGPO.Data;

namespace nGGPO.Utils;

static class Async
{
    public static async ValueTask WhenAll(ValueList<ValueTask> source)
    {
        List<Exception>? exceptions = null;

        for (var i = 0; i < source.Count; i++)
            try
            {
                await source[i].ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= new(source.Count);
                exceptions.Add(ex);
            }

        if (exceptions is not null)
            throw new AggregateException(exceptions);
    }
}
