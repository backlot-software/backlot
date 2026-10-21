using System.Linq;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Json;
using Backlot.Testing.UseCases.ComplexActor.Roles;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing.UseCases.ComplexActor;

/// <summary>
/// This fixture covers a use-case used by real world implementations of backlot.
/// Testcases contain a complex JSON object which is represented by a typed self or IRole implementation.
/// It also covers a situation where a construct is used in the JSON which on its self does not implement an IRole,
/// in these situations the __construct is ignored.
/// </summary>

/// <summary>
/// As a Backlot developer,
/// I want to present complex JSON actors as roles with or without valid construction metadata,
/// so that properties are preserved and legacy data remains compatible.
/// </summary>
public class ComplexActorPresentation
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
    public void GivenComplexJsonActor_WhenPresentedWithConstruct_ThenPresentedAsSelf()
    {
        // Act
        var asInterface = _strjsn.Presents<ICustomerRegistration>();
        var asSelf = _strjsn.Presents<CustomerRegistrationSelf>();
        
        // Assert
        Assert.Multiple(() =>
        {
          // Type assertions
          Assert.That(asInterface, Is.TypeOf<CustomerRegistrationSelf>());
          Assert.That(asSelf, Is.TypeOf<CustomerRegistrationSelf>());
          
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
        });
    }
    
    [Test]
    public void GivenComplexJsonActor_WhenPresentedWithoutConstruct_ThenPresentedUsingJsonInterceptorReturningProxyAndSelf()
    {
      // Arrange
      var jObj = JObject.Parse(_strjsn);
      jObj.Remove("__Construct");
      var strJsnNoConstruct = jObj.ToString(); // to plain text again to align it with all other tests in this use-case

      // Act
      var asInterface = strJsnNoConstruct.Presents<ICustomerRegistration>();
      var asSelf = strJsnNoConstruct.Presents<CustomerRegistrationSelf>();
        
      // Assert
      Assert.Multiple(() =>
      {
        // Type assertions
        Assert.That(asInterface, Is.InstanceOf<IJProxy>()); // it's proxied
        Assert.That(asSelf, Is.TypeOf<CustomerRegistrationSelf>()); // it's a self'
        
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
      });
    }
    
    /// <summary>
    /// This test needs to represent a JSON object with a construct that is not a role itself. This can occur in databases with a previous version of backlot.
    /// This test ensures we are backwards compatible.
    /// </summary>
    [Test]
    public void GivenComplexJsonActorWithLegacyNoneSelfConstruct_WhenPresentedAsRole_ThenPresentedUsingJsonInterceptorAndCreatesJsonProxy()
    {
      // Arrange
      var jObj = JObject.Parse(_strjsn);
      jObj["__Construct"] = "Backlot.Testing.UseCases.ComplexActor.Roles.CustomerRegistrationNoRole, Backlot.Testing";
      var strJsnNoRoleConstruct = jObj.ToString();
      
      // Act
      var role = strJsnNoRoleConstruct.Presents<ICustomerRegistration>();
        
      // Assert
      Assert.Multiple(() =>
      {
        Assert.That(role, Is.InstanceOf<IJProxy>()); // it's proxied'
        Assert.That(role, Is.InstanceOf<ICustomerRegistration>()); // and represents a role
        
        // Property assertions
        Assert.That(role.RequestProductType, Is.EqualTo(ProductType.Flexible));
        Assert.That(role.RequestFollowUp, Is.EqualTo(FollowUp.Private));
        Assert.That(role.Me.FirstName, Is.EqualTo("Alice Johnson"));
        Assert.That(role.PrimaryContact.FirstName, Is.EqualTo("Bob Johnson"));
        Assert.That(role.ShippingAddress?.Street, Is.EqualTo("123 Main St"));
        Assert.That(role.OtherContacts.Count(), Is.EqualTo(2));
      });
    }
}