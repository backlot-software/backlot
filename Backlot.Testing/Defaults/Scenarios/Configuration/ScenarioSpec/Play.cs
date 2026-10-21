using System.Threading.Tasks;
using NUnit.Framework;

namespace Backlot.Testing.Defaults.Scenarios.Configuration.ScenarioSpec;

/// <summary>
/// Tests the machine-readable contract Backlot emits for its endpoints.
/// These tests execute ScenarioSpec.Play() and verify the generated
/// TypeSpec contract. The underlying contract generation is handled
/// by TypeSpecEmitter.Emit().
/// </summary>
/// <remarks>
/// These are characterization tests rather than a full snapshot: the document changes whenever a
/// role or scenario is added to this project, so asserting the whole text would fail for reasons
/// that have nothing to do with the emitter. What is asserted instead is the fidelity a code
/// generator depends on -- the properties an example-based document cannot carry.
/// </remarks>
public class Play
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }
    
    private static int Occurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, System.StringComparison.Ordinal);
             i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, System.StringComparison.Ordinal))
            count++;

        return count;
    }
    
    [Test]
    public async Task Play_LoadedScenarios_EmitsTheDeclaredPreamble()
    {
        // Act
        var spec = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(spec, Does.Contain("import \"@typespec/http\";"));
            Assert.That(spec, Does.Contain("using Http;"));
            Assert.That(spec, Does.Contain("namespace Backlot;"));
        });
    }
    
    [Test]
    public async Task Play_LoadedScenarios_EmitsTheDeclaredSharedResponseEnvelopeOnce()
    {
        // Act
        var spec = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();

        // Assert
        Assert.Multiple(() =>
        {
            // Declared once and referenced by every operation -- the reason TypeSpec is emitted
            // rather than OpenAPI, which has no generics and repeats this per operation.
            Assert.That(spec, Does.Contain("model Envelope<T> {"));
            Assert.That(Occurrences(spec, "model Envelope<T> {"), Is.EqualTo(1));
        });
    }
    
    [Test]
    public async Task Play_ACalculatedField_EmitsCalculatedFieldReadOnlyRatherThanOmittingIt()
    {
        // IPersist.LastModified is [Calculated]: never accepted on a request, always present on a
        // response. One model serves both directions through visibility, which is what stops a
        // generated client from demanding a field the caller must not send.
        
        // Act
        var spec = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();

        // Assert
        Assert.That(spec, Does.Contain("@visibility(Lifecycle.Read) LastModified?: offsetDateTime;"));
    }
    
    [Test]
    public async Task Play_RoleReachedFromAScenario_EmitsRoleAsNamedModelWithOptionalityIntact()
    {
        // The Persist scenario plays IPersist, so Persist reaches the contract as a model of its
        // own rather than inlined -- which is what lets a recursive role graph terminate without
        // being truncated to an empty object.
        
        // Act
        var spec = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(spec, Does.Contain("model Persist {"));

            // Uid carries [Required]; Name does not. An example-based document cannot express this
            // difference at all, and it is what a generated client turns into `Name?: string`.
            Assert.That(spec, Does.Contain("Uid: string;"));
            Assert.That(spec, Does.Contain("Name?: string;"));
        });
    }
    
    [Test]
    public async Task Play_DirectorScenario_UsesGet()
    {
        // Act
        var spec = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();

        // Assert
        Assert.That(spec, Does.Contain("@get"));
    }
    
    [Test]
    public async Task Play_DirectorScenario_SendsNoBody()
    {
        // Act
        var spec = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();

        // Assert
        // A director scenario takes no role input, so its operation has an empty parameter list.
        Assert.That(spec, Does.Contain("Scenarios(): Envelope<"));
    }
    
    [Test]
    public async Task Play_DirectorScenario_DoesNotEmitDirectorModel()
    {
        // Act
        var spec = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();

        // Assert
        // the director's own properties are injected services, never contract data.
        Assert.That(spec, Does.Not.Contain("model Director {"));
    }
    
    [Test]
    public async Task Play_PlayedTwice_ReturnsTheSameTextSoDriftIsDiffable()
    {
        // Act
        var first = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();
        var second = await Backlot.Defaults.Scenarios.Configuration.ScenarioSpec.Play();

        // Assert
        Assert.That(second, Is.EqualTo(first));
    }
}