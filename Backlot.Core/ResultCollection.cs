using System.Collections;
using System.Collections.Generic;

namespace Backlot.Core;

public interface IResultCollection
{
    public IEnumerable Results { get; }
}

public class ResultCollection<T> : IResultCollection
{
    IEnumerable IResultCollection.Results => Results;
    public IEnumerable<T> Results { get; init; }
}

public class PagedResultCollection<T> : ResultCollection<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}