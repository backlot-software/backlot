using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Testing.UseCases.ComplexActor.Roles;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing.UseCases.ComplexActor;

/// <summary>
/// These unit-test does cover a use-case used by real world implementations of backlot.
/// It's containing a complex json object which is represented by a typed self or IRole implementation.
/// It also covers a situation where a construct is used in the json which on it self does not implement an IRole
/// , in these situations the __construct is ignored.
/// </summary>
public class Case
{
    private static string _strjsn = @"{
      ""OtherContacts"": [
        {
          ""FirstName"": ""Jane Doe"",
          ""DateOfBirth"": ""1985-06-15T00:00:00"",
          ""EmailAddress"": ""jane.doe@backlot.ws"",
          ""PhoneNumber"": ""555-123-4567""
        },
        {
          ""FirstName"": ""John Smith"",
          ""DateOfBirth"": ""1978-11-03T00:00:00"",
          ""EmailAddress"": ""john.smith@backlot.ws"",
          ""PhoneNumber"": ""555-987-6543""
        }
      ],
      ""RequestProductType"": ""Flexible"",
      ""RequestFollowUp"": 2,
      ""Name"": ""Alice Johnson"",
      ""DateOfBirth"": ""1990-04-22T00:00:00"",
      ""ShippingAddress"": {
        ""Street"": ""123 Main St"",
        ""City"": ""Springfield"",
        ""State"": ""IL"",
        ""ZipCode"": ""62704"",
        ""Country"": ""USA""
      },
      ""Me"": {
        ""FirstName"": ""Alice Johnson"",
        ""DateOfBirth"": ""1990-04-22T00:00:00"",
        ""EmailAddress"": ""alice.johnson@backlot.ws"",
        ""PhoneNumber"": ""555-555-5555""
      },
      ""PrimaryContact"": {
        ""FirstName"": ""Bob Johnson"",
        ""DateOfBirth"": ""1988-08-12T00:00:00"",
        ""EmailAddress"": ""bob.johnson@backlot.ws"",
        ""PhoneNumber"": ""555-111-2222""
      },
      ""__Construct"": ""Backlot.Testing.UseCases.ComplexActor.Roles.CustomerRegistrationSelf, Backlot.Testing"",
      ""__Skills"": [
        ""CustomerRegistration"",
        ""Persist"",
        ""Permission"",
        ""Role"",
        ""Uid""
      ]
    }";
    

    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    [Test]
    public void Presents_ComplexJsonActorWithConstructor_PresentedAsSelf()
    {
        #region ACT

        var asInterface = _strjsn.Presents<ICustomerRegistration>();
        var asSelf = _strjsn.Presents<CustomerRegistrationSelf>();
        
        #endregion
        
        // ASSERT
        
        // Type assertions
        Assert.That(asInterface is CustomerRegistrationSelf);
        Assert.That(asSelf is CustomerRegistrationSelf);
    
        // Property assertions
        Assert.That(asInterface.RequestProductType, Is.EqualTo(ProductType.Flexible));
        Assert.That(asInterface.RequestFollowUp, Is.EqualTo(FollowUp.Private));
        Assert.That(asInterface.Me.FirstName, Is.EqualTo("Alice Johnson"));
        Assert.That(asInterface.PrimaryContact.FirstName, Is.EqualTo("Bob Johnson"));
        Assert.That(asInterface.ShippingAddress?.Street, Is.EqualTo("123 Main St"));
        Assert.That(asInterface.OtherContacts.Count(), Is.EqualTo(2));
        
        Assert.That(asSelf.RequestProductType, Is.EqualTo(ProductType.Flexible));
        Assert.That(asSelf.RequestFollowUp, Is.EqualTo(FollowUp.Private));
        Assert.That(asSelf.Me.FirstName, Is.EqualTo("Alice Johnson"));
        Assert.That(asSelf.PrimaryContact.FirstName, Is.EqualTo("Bob Johnson"));
        Assert.That(asSelf.ShippingAddress?.Street, Is.EqualTo("123 Main St"));
        Assert.That(asSelf.OtherContacts.Count(), Is.EqualTo(2));
    }
    
    [Test]
    public void Presents_ComplexJsonActorWithoutConstruct_PresentedUsingJsonInterceptor()
    {
      #region ARRANGE
        
      var jobj = JObject.Parse(_strjsn);
      jobj.Remove("__Construct");
      var strjsnNoconstruct = jobj.ToString(); // to plain text again to align it with all other tests in this use-case
      
      #endregion

      #region ACT

      var asInterface = strjsnNoconstruct.Presents<ICustomerRegistration>();
      var asSelf = strjsnNoconstruct.Presents<CustomerRegistrationSelf>();
        
      #endregion

      // Type assertions
      Assert.That(asInterface is IJProxy); // it's proxied
      Assert.That(asSelf is CustomerRegistrationSelf); // it's a self'
    
      // Property assertions
      Assert.That(asInterface.RequestProductType, Is.EqualTo(ProductType.Flexible));
      Assert.That(asInterface.RequestFollowUp, Is.EqualTo(FollowUp.Private));
      Assert.That(asInterface.Me.FirstName, Is.EqualTo("Alice Johnson"));
      Assert.That(asInterface.PrimaryContact.FirstName, Is.EqualTo("Bob Johnson"));
      Assert.That(asInterface.ShippingAddress?.Street, Is.EqualTo("123 Main St"));
      Assert.That(asInterface.OtherContacts.Count(), Is.EqualTo(2));
        
      Assert.That(asSelf.RequestProductType, Is.EqualTo(ProductType.Flexible));
      Assert.That(asSelf.RequestFollowUp, Is.EqualTo(FollowUp.Private));
      Assert.That(asSelf.Me.FirstName, Is.EqualTo("Alice Johnson"));
      Assert.That(asSelf.PrimaryContact.FirstName, Is.EqualTo("Bob Johnson"));
      Assert.That(asSelf.ShippingAddress?.Street, Is.EqualTo("123 Main St"));
      Assert.That(asSelf.OtherContacts.Count(), Is.EqualTo(2));
    }
    
    /// <summary>
    /// This test needs to represent a json object with a construct that is not a role itself. This can occur in databases with a previous version of backlot.
    /// This test ensures we are backwards compatible.
    /// </summary>
    [Test]
    public void Presents_ComplexJsonActorWithLegacyNoneSelfConstruct_PresentedUsingJsonInterceptor()
    {
      #region ARRANGE

      var jobj = JObject.Parse(_strjsn);
      jobj["__Construct"] = "Backlot.Testing.UseCases.ComplexActor.Roles.CustomerRegistrationNoRole, Backlot.Testing";
      var strjsnNoRoleConstruct = jobj.ToString();
      
      #endregion

      #region ACT

      var role = strjsnNoRoleConstruct.Presents<ICustomerRegistration>();
        
      #endregion

      // Type assertions
      Assert.That(role is IJProxy); // it's proxied'
      Assert.That(role is ICustomerRegistration); // and represents a role
    
      // Property assertions
      Assert.That(role.RequestProductType, Is.EqualTo(ProductType.Flexible));
      Assert.That(role.RequestFollowUp, Is.EqualTo(FollowUp.Private));
      Assert.That(role.Me.FirstName, Is.EqualTo("Alice Johnson"));
      Assert.That(role.PrimaryContact.FirstName, Is.EqualTo("Bob Johnson"));
      Assert.That(role.ShippingAddress?.Street, Is.EqualTo("123 Main St"));
      Assert.That(role.OtherContacts.Count(), Is.EqualTo(2));
    }
}