using Backlot.Core.Services;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Http;

namespace Backlot.Services.RavenDb;

public class RavenUnitOfWork : IUnitOfWork, IDisposable
{
    public readonly IAsyncDocumentSession AsyncSession;
    public IDictionary<string, RavenCommand<PutResult>> Commands;

    public RavenUnitOfWork()
    {
        AsyncSession = Db.Store.OpenAsyncSession();
        Commands = new Dictionary<string, RavenCommand<PutResult>>();
    }

    public async Task Commit()
    {
        foreach (var command in Commands)
        {
            await AsyncSession.Advanced.RequestExecutor.ExecuteAsync(command.Value, AsyncSession.Advanced.Context);
        }

        Commands = new Dictionary<string, RavenCommand<PutResult>>();
        await AsyncSession.SaveChangesAsync();
    }

    public void Dispose()
    {
        Commands = null;
        AsyncSession?.Dispose();
    }
}