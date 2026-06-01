using Backlot.Core;

namespace Backlot.Defaults.Roles;

public struct SimpleQuery() : ISimpleQuery
{
    public string For { get; set; } = typeof(IRole).GetRoleName();
    public IEnumerable<Criteria> Criteria { get; set; } = new List<Criteria>();
    public int PageSize { get; set; } = 10;
    public int Page { get; set; } = 1;
    public string OrderBy { get; set; } = "";
    public DateTimeOffset? From { get; set; } = DateTimeOffset.UtcNow.AddYears(-1);
    public DateTimeOffset? Till { get; set; } = DateTimeOffset.UtcNow;
}

public struct SimpleQuery<TRole>() : ISimpleQuery
    where TRole : IRole
{
    public string For { get; set; } = typeof(TRole).GetRoleName();
    public IEnumerable<Criteria> Criteria { get; set; } = new List<Criteria>();
    public int PageSize { get; set; } = 10;
    public int Page { get; set; } = 1;
    public string OrderBy { get; set; } = "";
    public DateTimeOffset? From { get; set; } = DateTimeOffset.UtcNow.AddYears(-1);
    public DateTimeOffset? Till { get; set; } = DateTimeOffset.UtcNow;
}