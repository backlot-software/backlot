using System;
using System.Threading.Tasks;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.DependencyInjection;
using Backlot.Core.Services;
using Backlot.Testing.UseCases.NullAllowedScenarios.Roles;
using Backlot.Testing.UseCases.NullAllowedScenarios.Scenarios;
using NUnit.Framework;

namespace Backlot.Testing.UseCases.NullAllowedScenarios;

/// <summary>
/// Scenarios are build by the ScenarioBuilder. Role Parameters are given before play starts (as a collection or a single item),
/// or loaded from the repository when not defined but persisted before.
/// When the repository for some reason (f.e. access rights) does not return a related item and nullallowed is not set an exception is thrown.
/// With NullAllowed you can ensure a role is allowed to be null when the scenario starts.
/// Keep in mind that Validations are still executed against all Roles that are defined being public available within the scenario.
/// </summary>

/// <summary>
/// As a Backlot developer,
/// I want to configure scenarios for roles to be optional,
/// so that a scenario can execute when a related role is not available.
/// </summary>
public class OptionalRoleScenario
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public void GivenMissingRole_WhenScenarioWithoutNullAllowedIsPlayed_ThenArgumentExceptionIsThrown()
    {
        // Arrange
        
        var cardCode = new
        {
            Uid = "DD50AE10-5910-4C0C-8208-F8287D9220EC",
            CardCode = "9EEA3F40-202601",
            BardCode = "111111111111111"
        }.Presents<ICustomerCard>();
        
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await ProcessWithoutNullAllowed.Play(cardCode);
        });
    }
    
    [Test]
    public async Task GivenMissingRole_WhenScenarioWithNullAllowedIsPlayed_ThenScenarioExecutes()
    {
        // Arrange
        var cardCode = new
        {
            Uid = "DD50AE10-5910-4C0C-8208-F8287D9220EC",
            CardCode = "9EEA3F40-202601",
            BardCode = "111111111111111"
        }.Presents<ICustomerCard>();
        
        // Act
        var result = await ProcessWithNullAllowed.Play(cardCode);

        // Assert
        Assert.That(result, Is.True);
    }
    
    [Test]
    public async Task GivenPersistedRelatedRoles_WhenScenarioWithoutNullAllowedIsPlayed_ThenRelatedRoleIsResolvedAndScenarioExecutes()
    {
        // Arrange
        var card = new
        {
            Uid = "DD50AE10-5910-4C0C-8208-F8287D9220EC",
            CardCode = "9EEA3F40-202601",
            BardCode = "111111111111111"
        }.Presents<ICustomerCard>();
        
        var person = new
        {
            Uid = "EFF60EFC-CE37-4E76-97A4-89E83CC9E7A9",
            FirstName = "John",
            LastName = "Doe"
        }.Presents<IPersistedCardPerson>();
        
        await ServiceLocator.Get<IPersistedRoleRepository>().Persist(card);
        await ServiceLocator.Get<IPersistedRoleRepository>().Persist(person);
        
        await ServiceLocator.Get<IRelationRepository>().Add(Relation.New(card.GetReference(), person.GetReference()));
        
        // Act
        var result = await ProcessWithoutNullAllowedPersistedRelation.Play(card);
        
        // Assert
        Assert.That(result,  Is.True);
    }
}