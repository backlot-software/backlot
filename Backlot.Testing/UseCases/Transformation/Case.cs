using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Backlot.Core;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Abstraction.Roles;
using Backlot.Core.Services;
using Backlot.Defaults.Roles;
using Backlot.Testing.Core;
using NSubstitute;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing.UseCases.Transformation;

/// <summary>
/// Unit test for testing transformations a role made during time.
/// </summary>
public class Case
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
    public void Setup(Func<IEnumerable<Revision>> revisions)
    {
        Initialize.Setup(registerRepos: builder =>
        {
            builder.Register(_ =>
            {
                var repo = Substitute.For<IPersistedRoleRepository>();
                
                repo.GetRevisions<IPersist>(Arg.Any<string>())
                    .Returns(_ => revisions());

                return repo;
            }).As<IPersistedRoleRepository>();
            
            builder.RegisterType<MemoryRelationRepository>().As<IRelationRepository>();
        });
    }

    #endregion
    
    [Test]
    public async Task Transformation_UsingASelfRole_TransformationHasToBeReturned()
    {
        #region ARRANGE

        Setup(SetupSelfRevisions);
        
        var seek = Acting.New<ISeek>();
        seek.Command = "fwd";
        seek.For = SetupSelfRevisions().First().Reference;

        #endregion

        #region ACT

        var result = await Defaults.Scenarios.Persistance.Transformation.Play(seek);
        
        #endregion

        #region ASSERT
        
        Assert.That(result != null);
        
        #endregion
    }
    
    [Test]
    public async Task Transformation_UsingARevisionWithANullValue_TransformationIsReturnedIncludingThisNullValue()
    {
        #region ARRANGE

        Setup(SetupJsonWithNullRevisions);
        
        var seek = Acting.New<ISeek>();
        seek.Command = "fwd";
        seek.For = SetupJsonWithNullRevisions().First().Reference;

        #endregion

        #region ACT
        
        var result = await Defaults.Scenarios.Persistance.Transformation.Play(seek);
        
        #endregion
        
        #region ASSERT
        
        Assert.That(result["Number2"]?.Value<int>() == 3); // The unknown extras property is still there.
        Assert.That(result["Operation"]?.Value<string>() == null);
        
        #endregion
    }
}