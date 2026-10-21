using System.Collections.Generic;
using Backlot.Testing.Core;
using Backlot.Testing.Shared.TestModels;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Backlot.Testing.Defaults.Instructing.MustachExpressionEngine;

public class Execute
{
    private Backlot.Defaults.Instructing.MustachExpressionEngine _expressionEngine;
    [SetUp]
    public void Setup()
    {
        _expressionEngine = new Backlot.Defaults.Instructing.MustachExpressionEngine();
    }
    
    [Test]
    public void Execute_WithTypedActor_CalculatedValue()
    {
        // Arrange
        const string expression = "{{Number1}}_{{Number2}}_{{Operation}}_{{Uid}}";
        
        var actor = new Formula()
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            Uid = "023d02600ded"
        };
        
        // Act
        var value = _expressionEngine.Execute(expression, actor);
        
        // Assert
        Assert.That(value, Is.EqualTo("7_9_sum_023d02600ded"));
    }
    
    [Test]
    public void Execute_WithJSONActor_CalculatedValue()
    {
        // Arrange
        const string expression = "{{Number1}}_{{Number2}}_{{Operation}}_{{Uid}}";

        var actor = JObject.FromObject(new
        {
            Number1 = 7,
            Number2 = 9,
            Operation = "sum",
            Uid = "023d02600ded"
        });
        
        // Act
        var value = _expressionEngine.Execute(expression, actor);
        
        // Assert
        Assert.That(value, Is.EqualTo("7_9_sum_023d02600ded"));
    }
    
    [Test]
    public void Execute_WithDictionaryActor_CalculatedValue()
    {
        // Arrange
        const string expression = "{{Number1}}_{{Number2}}_{{Operation}}_{{Uid}}";
        
        var actor = new Dictionary<string, object>
        {
            { "Number1", 7 },
            { "Number2", 9 },
            { "Operation", "sum" },
            { "Uid", "023d02600ded" }
        };
        
        // Act
        var value = _expressionEngine.Execute(expression, actor);
        
        // Assert
        Assert.That(value, Is.EqualTo("7_9_sum_023d02600ded"));

    }
    
}