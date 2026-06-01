using System;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;
using LDB = LiteDB;

namespace Backlot.Services.LiteDB;

internal static class Db
{
    // TODO: REMOVE: private const string TypeDiscriminator = Meta.__Construct;
    private static string ConnectionString =>
        ServiceLocator.Get<IConfigurationManager>().Get<Settings>(s => s.ConnectionString);

    private static readonly Lazy<LDB.ILiteDatabase> LazyStore = new(Initialize);
    internal static LDB.ILiteDatabase Store => LazyStore.Value;

    private static LDB.ILiteDatabase Initialize()
    {
        return new LDB.LiteDatabase(ConnectionString);
    }
}
