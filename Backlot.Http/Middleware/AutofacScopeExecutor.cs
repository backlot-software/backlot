using System.Net;
using Autofac.Core.Lifetime;
using Autofac.Extensions.DependencyInjection;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;

namespace Backlot.Http.Middleware;

public class AutofacScopeExecutor : IMiddleware
{
    
    public async Task ExecuteAsync(MiddlewareContext context, Func<Task> next,
        CancellationToken cancellationToken = default)
    {
        if (context.CurrentInstanceServices() is not AutofacServiceProvider provider)
            throw new ArgumentException(
                $"context.InstanceServices is not from the correct provider type. {nameof(AutofacScopeExecutor)} can only handle {nameof(AutofacServiceProvider)}.");

        await using var scope = provider.LifetimeScope.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag);
        var scopedProvider = new AutofacServiceProvider(scope);
        context.ChangeInstanceServices(scopedProvider);
        ServiceLocator.Configure(scopedProvider);
        var uow = ServiceLocator.Get<IUnitOfWork>();
        
        await next();
                
        if(context.HttpResponseStatus() == HttpStatusCode.OK)
            await uow.Commit();
    }
}