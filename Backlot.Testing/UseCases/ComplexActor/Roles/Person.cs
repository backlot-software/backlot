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

public class PersonSelf : IPersonRole
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string Notes { get; set; }
    public bool Available { get; set; }
}