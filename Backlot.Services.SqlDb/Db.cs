using Backlot.Core.DependencyInjection;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace Backlot.Services.SqlDb;

/// <summary>
/// MS Sql Database.
/// </summary>
internal static class Db
{
    private static string ConnectionString =>
        ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.ConnectionString);

    internal static QueryFactory Store()
    {
        var connection = new SqlConnection(ConnectionString);
        return new QueryFactory(connection, new SqlServerCompiler());
    }
    
    /// <summary>
    /// Customized Serializer used for serializing an object.
    /// - inspried by RavenDb implementation.
    /// </summary>
    internal static JsonSerializer Serializer => Strategy.SerializeForPersistance; // canwrite - 

    /// <summary>
    /// Customized serializer used for de-serializing an object
    /// - inspried by RavenDb implementation.
    /// </summary>
    internal static JsonSerializer Deserializer => Strategy.DeSerializeFromTrustedSource; // canread - 
}