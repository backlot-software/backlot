// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Backlot.Http.Media.Formatters;

internal class JsonResponse<T>
{
    internal JsonResponse(T obj, long timeInMs)
    {
        Body = obj;
        TimeInMs = timeInMs;
    }

    public DateTimeOffset ExecutionTime => DateTimeOffset.Now;
    public long TimeInMs { get; }
    public T Body { get; set; }
    public string Status { get; set; } = null!;
}