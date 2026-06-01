using System.Threading.Tasks;

namespace Backlot.Core.Services;

/// <summary>
/// Unit of work implementation doing nothing. This Uow can be used when your implementation does not use or support the unit of work
/// </summary>
public class DummyUnitOfWork : IUnitOfWork
{
    public Task Commit()
    {
        return Task.CompletedTask;
    }
}
