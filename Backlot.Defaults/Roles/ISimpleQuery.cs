using Backlot.Core;

namespace Backlot.Defaults.Roles;

public interface ISimpleQuery : IRole
{
    /// <summary>
    /// Required rolename
    /// </summary>
    public string For { get; set; } // todo: rename this because it now conflicts with ISeekBase.For naming convention.
    
    /// <summary>
    /// Optional Criteria
    /// </summary>
    public IEnumerable<Criteria> Criteria { get; set; }
    
    /// <summary>
    /// Required pagesize
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Required pagenumber
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Optional Propertyname
    /// </summary>
    public string OrderBy { get; set; }
    
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? Till { get; set; }
}