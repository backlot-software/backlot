using Backlot.Core;

namespace Backlot.Demo.Azure.Roles;

public interface ISum : IPersist
{
    double Number1 { get; set; }
    double Number2 { get; set; }
}