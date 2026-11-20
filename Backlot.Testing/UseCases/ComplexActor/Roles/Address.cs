namespace Backlot.Testing.UseCases.ComplexActor.Roles;

public interface IAddress
{
    string Street { get; set; }
    string StreetNumber { get; set; }
    string ZipCode { get; set; }
    string City { get; set; }
}

public class Address : IAddress
{
    public string Street { get; set; }
    public string StreetNumber { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
}