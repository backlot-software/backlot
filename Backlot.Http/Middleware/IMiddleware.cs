namespace Backlot.Http.Middleware;

public interface IMiddleware
{
    Task ExecuteAsync(MiddlewareContext context,
        Func<Task> next,
        CancellationToken cancellationToken = default);
}