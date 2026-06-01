using Backlot.Core;
using Backlot.Core.Json;
using Backlot.Defaults.Instructing;

namespace Backlot.Demo.Azure.Roles;

[FieldInfoAlias("Uid", ["Id", "relationId"])]
public interface IPerson : IPersist
{
    [Calculated]
    string Fullname { get; set; }
    string Firstname { get; set; }
    string Lastname { get; set; }
    [Alias(["Adres", "Location"])]
    string Address { get; set; }
}