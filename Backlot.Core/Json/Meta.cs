using System.Diagnostics.CodeAnalysis;

namespace Backlot.Core.Json;

[SuppressMessage("ReSharper", "InconsistentNaming")] // we like to use a prefix of __
public static class Meta
{
    // Meta property prefix 
    public const string __ = "__";
   
    // __Constructs are used when 'self' roles are serialized as json.
    public const string __Construct = $"{__}Construct";
    
    // __Skills are used for saving all roletypes a certain object can represent.
    public const string __Skills = $"{__}{nameof(Loader.Skills)}";
    
    // __Permissions, the permissions an entity has.
    public const string __Permission = $"{nameof(IPermission.__Permission)}";
}