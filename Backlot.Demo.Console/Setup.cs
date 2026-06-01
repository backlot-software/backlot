using Autofac;
using Autofac.Extensions.DependencyInjection;
using Backlot.Core.DependencyInjection;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Microsoft.Extensions.Logging;

namespace Backlot.Demo.Console;

public static class Setup
{
    public static void ForUmbraco()
    {
        throw new NotImplementedException();
    } // etc..
    
    public static void ForConsoles()
    {
        var builder = new ContainerBuilder();
        
        builder.RegisterInstance(new LoggerFactory())
            .As<ILoggerFactory>();
        
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .SingleInstance();
        
        var configuration = new DictionaryConfiguration(new Dictionary<string, string>
        {
            { "Backlot.Services.SqlDb.Settings.ConnectionString", "Server=<HOST>;Database=<DBNAME>;User Id=<USERNAME>;Password=<PW>;TrustServerCertificate=True;" },
            { "Backlot.Services.LiteDB.Settings.ConnectionString", "mydata.db"}
        });

        var director = new Director(new LocalDiskStorage(), configuration, builder);
        director.Registration();
        director.Incept();
        
        // Building 

        var serviceProvider = new AutofacServiceProvider(builder.Build());
        ServiceLocator.Configure(serviceProvider);
    }
}