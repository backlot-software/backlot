using System.Security.Cryptography.X509Certificates;
using Backlot.Core;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Json;
using Backlot.Core.Json.Serialization.Newtonsoft;
using Backlot.Core.Services;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;
using Raven.Client.Json.Serialization.NewtonsoftJson;

namespace Backlot.Services.RavenDb;


/// <summary>
/// Singleton
/// </summary>
internal static class Db
{
    //about collections;
    //https://ravendb.net/docs/article-page/5.3/csharp/studio/database/documents/documents-and-collections
    //https://ravendb.net/docs/article-page/5.3/csharp/client-api/faq/what-is-a-collection

    // reserved backlot meta data fields:
    internal const string Checksum = "Checksum";
    internal const string Pcl = Meta.__Permission;
    // reserved raven db meta data fields:
    internal const string MetaData = "@metadata";
    internal const string Collecion = "@collection";
    internal const string LastModified = "@last-modified";
    
    private static string ServerUrl => ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.ServerUrl);
    private static string DatabaseName => ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.DatabaseName);

    private static X509Certificate2 _certificate2;
    private static X509Certificate2 Certificate
    {
        get
        {
            if (_certificate2 == null)
            {
                var base64 = ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.X509Certificate2);
                if (string.IsNullOrEmpty(base64))
                    return null;

                var bytes = Convert.FromBase64String(base64);
                _certificate2 = new X509Certificate2(bytes, "", X509KeyStorageFlags.MachineKeySet); //machinekey set is used for azure functions. Otherwise a "The system cannot find the file specified." can occur
            }

            return _certificate2;
        }
    }

    internal const string RoleCollectionName = "Roles";
    
    private static readonly Lazy<IDocumentStore> LazyStore = new(Initialize);
    internal static IDocumentStore Store => LazyStore.Value;
    
    internal static JsonSerializer Serializer;
    internal static JsonSerializer DeSerializer;
    
    private static IDocumentStore  Initialize()
    {
        Serializer = Strategy.SerializeForPersistance;
        DeSerializer = Strategy.DeSerializeFromTrustedSource;
        
        var store = new DocumentStore
        {
            Urls = [ServerUrl],
            Database = DatabaseName,
            Certificate = Certificate,
            Conventions = new DocumentConventions
            {
                
                FindIdentityProperty = info => info.Name == nameof(IPersist.Uid),
                FindIdentityPropertyNameFromCollectionName = _ => nameof(IPersist.Uid),
                FindCollectionName = type =>
                {
                    if (typeof(IRole).IsAssignableFrom(type))
                    {
                        return RoleCollectionName;
                    }

                    return type?.Name;
                },
                Serialization = new NewtonsoftJsonSerializationConventions //raven is using custom serializer and not Backlot.Core.Json because f.e. __Construct meta data is something that is handled by raven itself.
                {
                    CustomizeJsonSerializer = serializer => // canwrite
                    {
                        serializer.ContractResolver = Serializer.ContractResolver;
                        serializer.TypeNameHandling = Serializer.TypeNameHandling;

                        foreach (var converter in Serializer.Converters)
                        {
                            serializer.Converters.Add(converter);
                        }
                    },
                    CustomizeJsonDeserializer = serializer => // canread
                    {
                        //TODO: 29/10/2025 - because contract resolver was never set, this is kept away from now, but ideally this should be set, consider to do so.
                        serializer.TypeNameHandling = DeSerializer.TypeNameHandling;
                        
                        foreach (var converter in DeSerializer.Converters)
                        {
                            serializer.Converters.Add(converter);
                        }
                    },
                }
            }
        }.Initialize();
        
        IndexCreation.CreateIndexes(typeof(Db).Assembly, store);

        return store;
    }
}

// ReSharper disable once ClassNeverInstantiated.Global