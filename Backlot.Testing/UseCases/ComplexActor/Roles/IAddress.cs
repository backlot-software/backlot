namespace Backlot.Testing.UseCases.ComplexActor.Roles;

public interface IAddress
{
    string Street { get; set; }
    string StreetNumber { get; set; }
    string ZipCode { get; set; }
    string City { get; set; }
}