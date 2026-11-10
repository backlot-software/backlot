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
    
    public class CustomerRegistrationSelf : ICustomerRegistration
    {
        public string? __Permission { get; set; }
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
    
        // Example of practices where the Role defines a property to another interfaced role.
        public PersonSelf PrimaryContact { get; set; }
        /// <summary>
        /// role as property = not possible within _selfs
        /// </summary>
        IPersonRole ICustomerRegistration.PrimaryContact
        {
            get => PrimaryContact; 
            set => PrimaryContact = (PersonSelf) value;
        }
    
        public DateTimeOffset? LastModified { get; set; }
    }
    
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