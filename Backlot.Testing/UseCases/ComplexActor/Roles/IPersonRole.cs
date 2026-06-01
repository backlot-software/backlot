using Backlot.Core;

namespace Backlot.Testing.UseCases.ComplexActor.Roles;

public interface IPersonRole : IRole
{
    string FirstName { get; set; }
    string LastName { get; set; }
    string Street { get; set; }
    string City { get; set; }
    string Notes { get; set; }
    bool Available { get; set; }
}