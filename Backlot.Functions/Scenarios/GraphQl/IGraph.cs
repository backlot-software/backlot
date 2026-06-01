using Backlot.Core;

namespace Backlot.Experimental.Functions.Scenarios.GraphQl;

public interface IGraph : IRole
{
    string Query { get; }
}