namespace Backlot.Testing.UseCases.ComplexActor.Roles;

public class Address : IAddress
{
    public string Street { get; set; }
    public string StreetNumber { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
}