#nullable enable
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Json;

namespace Backlot.Core;

/// <summary>
/// Optional interface to restrict entity access.
/// </summary>
[ExcludeValidation]
public interface IPermission : IRole
{
    /// <summary>
    /// FOR INTERNAL USE ONLY
    /// Try to use the extensionmethod IRole.Permission() instead.
    /// -----
    /// Unix inspired permission
    /// Pemission data is saved and managed within database metadata and not returned in any scenario result.
    /// Scenario
    /// format: $owner$group$public:$ownerid:$groupid
    /// -----
    /// Metafield managed by data repositories.
    /// </summary>
    [Calculated]
    // do not [JsonIgnore] this value, because it is needed in the actual actor. JsonInterceptor will remove it for its deepclone, but needs it at initialization.
    // ReSharper disable once InconsistentNaming
    string? __Permission { get;  protected internal set; }
}