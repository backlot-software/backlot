namespace Backlot.Studio.Services;

public class ApiEnvelope<T>
{
    public T? Body { get; set; }
    public string? Status { get; set; }
    public long TimeInMs { get; set; }
    public DateTimeOffset ExecutionTime { get; set; }
}
