namespace Backlot.Studio.Services;

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiEnvelope<T>
{
    public T? Body { get; set; }
    public string? Status { get; set; }
    public long TimeInMs { get; set; }
    public DateTimeOffset ExecutionTime { get; set; }
}
