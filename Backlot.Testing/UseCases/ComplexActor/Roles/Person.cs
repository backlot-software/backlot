namespace Backlot.Testing.UseCases.ComplexActor.Roles;

public class PersonSelf : IPersonRole
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string Notes { get; set; }
    public bool Available { get; set; }
}