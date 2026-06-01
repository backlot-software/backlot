using System.Threading.Tasks;

namespace Backlot.Core.Services;

public interface IUnitOfWork
{
    Task Commit();
}