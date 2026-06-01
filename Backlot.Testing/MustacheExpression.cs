using System.Collections.Generic;
using Backlot.Core.Abstraction.Actors;
using Backlot.Core.Services;
using Backlot.Defaults.Instructing;
using Backlot.Services.Filesystem.LocalDiskStorage;
using Backlot.Testing.Core;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace Backlot.Testing;

/// <summary>
/// Test to check if intercepting using the backlot interceptors works correctly.
/// </summary>
public class MustachExpression
{
    private MustachExpressionEngine _expressionEngine;
    [SetUp]
    public void Setup()
    {
        Initialize.Setup((builder) => new AliasDirector(new LocalDiskStorage(), Substitute.For<IConfigurationManager>(), builder));
        _expressionEngine = new MustachExpressionEngine();
    }

    [Test]
    public void Execute_WithTypedActor_CalulatedValue()
    {
        //todo: please use arrange act assert
        
        var value = _expressionEngine.Execute("{{Number1}}_{{Number2}}_{{Operation}}_{{Uid}}", new Formula()
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            Uid = "023d02600ded"
        });
        Assert.That(value == "7_9_sum_023d02600ded");
    }

    [Test]
    public void Execute_WithJSONActor_CalulatedValue()
    {
        //todo: please use arrange act assert
        
        var value = _expressionEngine.Execute("{{Number1}}_{{Number2}}_{{Operation}}_{{Uid}}", JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            Uid = "023d02600ded"
        }));
        Assert.That(value == "7_9_sum_023d02600ded");
    }

    [Test]
    public void Execute_WithDictionaryActor_CalculatedValue()
    {
        //todo: please use arrange act assert
        
        var value = _expressionEngine.Execute("{{Number1}}_{{Number2}}_{{Operation}}_{{Uid}}", new Dictionary<string, object>
        {
            {"Number1", 7},
            {"Number2", 9},
            {"Operation", "sum"},
            {"Uid", "023d02600ded"}
        });
        Assert.That(value == "7_9_sum_023d02600ded");
    }
    
    [Test]
    public void Presents_WithActorNotHavingAUidAndARoleAliasReferingToAExpressionEngine_UidIsCalculatedBasedOnExpressionEngine()
    {
        // arrange:
        var actor = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            Name = "sumexample",
            FormulaId = "12",
        });

        // act
        var role = actor.Presents<IFormula>();
        
        // assert:
        Assert.That(role.Uid == "12_sumexample!");
    }
}