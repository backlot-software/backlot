using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Autofac;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Services;
using Backlot.Defaults.Roles;
using Backlot.Testing.Defaults;
using Backlot.Testing.Defaults.Fakes;
using Backlot.Testing.Shared.Directors;
using Backlot.Testing.Shared.TestModels;
using NSubstitute;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing.UseCases.Transformation;

/// <summary>
/// Unit tests for testing transformations a role made during time.
/// </summary>

/// /// <summary>
/// As a Backlot developer,
/// I want to compare self-role and JSON revisions using forward transformations,
/// so that changed property values are returned, including values set to null.
/// </summary>
public class RoleTransformation
{

    #region Setup
    private static IEnumerable<Revision> SetupSelfRevisions()
    {
        var revision1 = new FormulaSelf
        {
            Number1 = 1,
            Operation = "sum",
            Number2 = 2,
            Number3 = 3,
            Uid = "595aab9b4c024c15bc49b59c16661a86",
            LastModified = DateTimeOffset.Now.AddDays(-1)
        }.Presents<IFormula>();
        
        var revision2 = new FormulaSelf
        {
            Number1 = 2,
            Operation = "div",
            Number2 = 2,
            Number3 = 3,
            Uid = "595aab9b4c024c15bc49b59c16661a86",
            LastModified = DateTimeOffset.Now
        }.Presents<IFormula>();

        var revisions = new List<Revision>()
        {
            new()
            {
                Checksum = revision2.GetChecksum(),
                Content = revision2,
                Reference = revision2.GetReference()
            },
            
            new()
            {
                Checksum = revision1.GetChecksum(),
                Content = revision1,
                Reference = revision1.GetReference()
            }
        };

        return revisions;
    }
    
    private static IEnumerable<Revision> SetupJsonWithNullRevisions()
    {
        var revision1 = "{\"Uid\":\"8f82dbe7a65040c7b53b1ca775e435e8\",\"Number1\":1,\"Number2\":2,\"Operation\":\"sum\"}".Presents<IFormula>();
        var revision2 = "{\"Uid\":\"8f82dbe7a65040c7b53b1ca775e435e8\",\"Number1\":1,\"Number2\":3,\"Operation\":null}".Presents<IFormula>();
        
        var revisions = new List<Revision>()
        {
            new()
            {
                Checksum = revision2.GetChecksum(),
                Content = revision2,
                Reference = revision2.GetReference()
            },
            
            new()
            {
                Checksum = revision1.GetChecksum(),
                Content = revision1,
                Reference = revision1.GetReference()
            }
        };

        return revisions;
    }
    
    // not [setup] attribute, because the setup differs per test.
    private static void Setup(Func<IEnumerable<Revision>> revisions)
    {
        var repo = Substitute.For<IPersistedRoleRepository>();
        repo.GetRevisions<IPersist>(Arg.Any<string>())
                    .Returns(_ => revisions());
        
        Defaults.Initialize.BuildApp(builder =>
            builder.ConfigureBacklot(
                (config, b) => new PermissionDirector(
                    new FakeStorage(), config, b),
                configureContainer: b =>
                {
                    b.RegisterInstance(repo)
                        .As<IPersistedRoleRepository>();

                    b.Register(_ => new ChecksumBuilder(
                            MD5.Create().ComputeHash))
                        .As<IChecksumBuilder>();
                }));
    }

    #endregion
    
    [Test]
    public async Task GivenSelfRoleRevisions_WhenForwardTransformationIsPlayed_ThenTransformationHasToBeReturned()
    {
        // Arrange
        Setup(SetupSelfRevisions);
        
        var seek = Acting.New<ISeek>();
        seek.Command = "fwd";
        seek.For = SetupSelfRevisions().First().Reference;

        // Act
        var result = await Backlot.Defaults.Scenarios.Persistance.Transformation.Play(seek);
        
        // Assert
        Assert.That(result, Is.Not.Null);
    }
    
    [Test]
    public async Task GivenJsonRevisionWithNullValue_WhenForwardTransformationIsPlayed_ThenTransformationIsReturnedIncludingNullValue()
    {
        // Arrange
        Setup(SetupJsonWithNullRevisions);
        
        var seek = Acting.New<ISeek>();
        seek.Command = "fwd";
        seek.For = SetupJsonWithNullRevisions().First().Reference;

        // Act
        var result = await Backlot.Defaults.Scenarios.Persistance.Transformation.Play(seek);
        
        // Assert
        Assert.That(result["Number2"]?.Value<int>(), Is.EqualTo(3));
        Assert.That(result["Operation"]?.Value<string>(), Is.Null);
    }
}