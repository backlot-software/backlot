using Backlot.Core;

namespace Backlot.Demo.Azure.Roles;

public interface IResult : IPersist
{
    float Outcome { get; set; }
    string Info { get; set; }
}