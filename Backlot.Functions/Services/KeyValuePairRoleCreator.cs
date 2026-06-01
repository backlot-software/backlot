using System.Text.RegularExpressions;
using System.Web;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Actors.RoleCreation;

namespace Backlot.Functions.Services;

using System;
using System.Linq;

/// <summary>
/// Role creator where the actor is based on a querystring / application/x-www-form-urlencoded like string.
/// </summary>
// ReSharper disable once UnusedType.Global
public class KeyValuePairRoleCreator : IRoleCreator
{
    public int Priority => 110;
    
    public bool CanCreate<TRole>(object actor) where TRole : IRole
    {

        var str = actor as string;
        if (str == null) return false;
        return Regex.IsMatch(str, @"\w*=\w*&?");
    }

    public TRole Create<TRole>(object actor, bool checkCanCreate=true) where TRole : IRole
    {
        if (checkCanCreate && !CanCreate<TRole>(actor))
        {
            throw new ArgumentException($"This role creator {nameof(DictionaryRoleCreator)} cannot create a role by using the given actor. Please use {nameof(CanCreate)} to avoid this exception.");
        }
        
#pragma warning disable CS8604 // Possible null reference argument. // we checked this before within CanCreate
        var parsed = HttpUtility.ParseQueryString(actor as string);
#pragma warning restore CS8604 // Possible null reference argument.
        var dic = parsed.AllKeys
            .ToDictionary(key => key!, key => parsed[key] as object);

        return DictionaryInterceptor.Generate<TRole>(dic);
    }
}