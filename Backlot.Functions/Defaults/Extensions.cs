using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Backlot.Functions.Defaults;

public static class Extensions
{
    public static ILoggingBuilder AddSerilog(this ILoggingBuilder builder, IConfiguration configuration, Func<LoggerConfiguration, LoggerConfiguration> configure, 
        LogEventLevel level = LogEventLevel.Warning)
    {
        var config = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            // ServerId mostly unique per customer. When not given alwas (00D3F1ND, NOTDEFINED)
            .Enrich.WithProperty("ChpServerId", configuration["Backlot.ID"] ?? "00D3F1ND")
            // environment used for loading configurations (such as <environment>jsonsettings.json)
            .Enrich.WithProperty("ChpEnvironment", configuration["Backlot.Environment"] ?? "local")
            .Enrich.FromLogContext();
            
        configure(config);
            
        var logger = config.CreateLogger();

        Log.Logger = logger;
        builder.Services.AddLogging(lb => lb.AddSerilog(logger, dispose: true));

        return builder;
    }
}