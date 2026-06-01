using System;
using System.Collections.Generic;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Backlot.Testing.UseCases.ComplexActor.Roles;


/// <summary>
/// Does use the jsoninterceptor, just like anonymous objects do.
/// </summary>
public class CustomerRegistrationNoRole
{
    public string Uid { get; set; }
    
    /// <summary>
    /// Typed objects implementing a _self role
    /// </summary>
    public IEnumerable<PersonSelf> OtherContacts { get; set; }
    
    /// <summary>
    /// Enums with string in jsn test we represent these as string
    /// </summary>
    public ProductType RequestProductType { get; set; }
    
    /// <summary>
    /// Enums with int, in jsn test we represent these as an int.
    /// </summary>
    public FollowUp RequestFollowUp { get; set; }
    
    public string Name { get; set; }
    
    public DateTime? DateOfBirth { get; set; }
    
    /// <summary>
    /// None role, with interface
    /// </summary>
    public Address? ShippingAddress { get; set; }
    
    /// <summary>
    /// _self role as property
    /// </summary>
    public PersonSelf Me { get; set; }
    
    public PersonSelf PrimaryContact { get; set; }
    
    public DateTimeOffset? LastModified { get; set; }
}