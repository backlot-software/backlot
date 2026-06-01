using System;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;
using StackExchange.Redis;

namespace Backlot.Authentication.BuiltIn.Redis;

public class Db
{
    private static readonly Lazy<IDatabase> LazyDb = new(Initialize);
    
    internal static IDatabase Database => LazyDb.Value;
    internal static readonly ConnectionMultiplexer Connection = ConnectionMultiplexer.Connect(ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.ServerUrl));
    private static IDatabase Initialize()
    {
        return Connection.GetDatabase();
    }
}