using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backlot.Core.DependencyInjection;

public static class ServiceLocator
{
    public static IServiceProvider Current => Scoped?.Value?.Item2;
    
    private static readonly AsyncLocal<Tuple<string, IServiceProvider>> Scoped = new();

    public static void Configure(IServiceProvider sl)
    {
        Scoped.Value =
            new Tuple<string, IServiceProvider>(Guid.NewGuid().ToString("n").Substring(0, 8), sl);
    }
    
    public static T GetLog<T>() where T: ILogger
    {
        return Current.GetService<T>();
    }
    
    public static T Get<T>()
    {
        return Current.GetService<T>();
    }
    
    public static object Get(Type type)
    {
        return Current.GetService(type);
    }
    
    public static object[] GetAllFor(Type type)
    {
        return Current.GetServices(type).ToArray();
    }

    public static T[] GetAllFor<T>()
    {
        return Current.GetServices<T>().ToArray();
    }
}