using System.Reflection;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Scenarios;
using Backlot.Core.Json;
using Backlot.Core.Security;
using Backlot.Defaults.Scenarios.Configuration.Models;

namespace Backlot.Defaults.Scenarios.Configuration;

[Scenario(typeof(Roles), access: [Access.Everyone])]
public class Roles : DirectorScenario<Roles, IEnumerable<RoleResultItem>>
{
    public Roles(IDirector role) : base(role)
    {
    }

    protected override async Task<IEnumerable<RoleResultItem>> ExecAsync()
    {
        var roles = Loader.AllRoles
            .Select(role => new RoleResultItem
            {
                Role = role.GetRoleName(),
                Fields = role.GetFieldInfo().Select(fld =>
                {
                    var chrs = fld.Characteristics
                        // only characteristics of fields having no sensitive data
                        // where attributes inherit from; ValidationAttribute or FieldCharacteristicAttribute
                        .Select(a => // return all attributes as characteristics (if any) for the corresponding field.
                        {
                            var str = a.GetType().FriendlyName();
                            return new CharacteristicResultItem
                            {
                                Characteristic = str?[..^"attribute".Length],
                                Parameters = a.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Select(p => new ParameterResultItem()
                                    {
                                        Name = p.Name,
                                        Value = p.GetValue(a)
                                    })
                                    // all public properties of the characteristic of type (bool (only true), int, string, double wihch are not null
                                    .Where(p =>
                                    {
                                        if (p.Value == null) return false;
                                        var valtype = p.Value.GetType();

                                        if (valtype.IsPrimitive || valtype == typeof(string))
                                        {
                                            return p.Value is bool b ? b : true;
                                        }

                                        return false;
                                    })
                            };
                        }).ToList();

                    // for the future optionally add more characteristics per field based on "other" configurations.

                    return new FieldResultItem // get all public fields for the role
                    {
                        Field = fld.Name,
                        FieldType = fld.FieldType,
                        Type = fld.FieldType.FriendlyName(), // role type.
                        Characteristics = chrs
                    };
                }).Where(f => !new[] { Meta.__Permission, Meta.__Skills, Meta.__Construct }.Contains(f.Field, StringComparer.InvariantCultureIgnoreCase)),
                Skills = Acting.New(role)
                    .Skills()
                    .Where(skl =>
                    {
                        // all skills, but not the ones we already know by default (system roles and the role itself)
                        return !new[] { "Role", "Uid", "Permission", role.FriendlyName() }
                            .Contains(skl, StringComparer.InvariantCultureIgnoreCase);
                    })
            });

        // filter for roles only used in scenarios the current user has access to
        var scenarios = (await Scenarios.Play()).ToList();

        // result of all roles only equal the the scenario Result, one of the scenario Roles or where the skill of role is of the role type.
        var result =
            UserContext.Current.IsInGroup(Access.Admin)
                ? roles //admins can see all roles no matter what 
                : // by default only the exact roles used in) these inheriting roles are not visible here when not used explicilty in a scenario.
                // this is because otherwise all roles become visible anyway because most do inherit from persit.
                roles.Where(rl => scenarios.Any(
                    sc => sc.Result == rl.Role ||
                          sc.Roles.Contains(rl.Role))
                ).ToList();
        
        return result;
    }
}