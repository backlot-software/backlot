using System;
using System.Collections.Generic;
using Backlot.Core;

namespace Backlot.Testing.UseCases.ComplexActor.Roles;

public interface ICustomerRegistration : IPersist
{
    
    /// <summary>
    /// Typed enumerable of objects implementing a _self role
    /// </summary>
    IEnumerable<PersonSelf> OtherContacts { get; set; }

    /// <summary>
    /// Enums with string in jsn test we represent these as string
    /// </summary>
    ProductType RequestProductType { get; set; }

    /// <summary>
    /// Enums with int, in jsn test we represent these as an int.
    /// </summary>
    FollowUp RequestFollowUp { get; set; }
    
    /// <summary>
    /// nullable date times
    /// </summary>
    DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// None role, with interface
    /// </summary>
    Address? ShippingAddress { get; set; }
    
    /// <summary>
    /// _self role as property
    /// </summary>
    PersonSelf Me { get; set; }
    
    /// <summary>
    /// role as property
    /// </summary>
    IPersonRole PrimaryContact { get; set; }
}