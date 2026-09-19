using System.Linq;
using System.Threading.Tasks;
using Backlot.Defaults.Scenarios.Configuration;
using NUnit.Framework;

namespace Backlot.Testing;

/// <summary>
/// Tests the machine-readable contract Backlot emits for its endpoints.
/// </summary>
/// <remarks>
/// These are characterization tests rather than a full snapshot: the document changes whenever a
/// role or scenario is added to this project, so asserting the whole text would fail for reasons
/// that have nothing to do with the emitter. What is asserted instead is the fidelity a code
/// generator depends on -- the properties an example-based document cannot carry.
/// </remarks>
public class TypeSpec
{
    [SetUp]
    public void Setup()
    {
        Initialize.Setup();
    }

    [Test]
    public async Task Emit_ForLoadedScenarios_DeclaresThePreambleAndTheSharedResponseEnvelope()
    {
        var spec = await ScenarioSpec.Play();

        Assert.Multiple(() =>
        {
            Assert.That(spec, Does.Contain("import \"@typespec/http\";"));
            Assert.That(spec, Does.Contain("using Http;"));
            Assert.That(spec, Does.Contain("namespace Backlot;"));

            // Declared once and referenced by every operation -- the reason TypeSpec is emitted
            // rather than OpenAPI, which has no generics and repeats this per operation.
            Assert.That(spec, Does.Contain("model Envelope<T> {"));
            Assert.That(Occurrences(spec, "model Envelope<T> {"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Emit_ForACalculatedField_MarksItReadOnlyRatherThanOmittingIt()
    {
        // IPersist.LastModified is [Calculated]: never accepted on a request, always present on a
        // response. One model serves both directions through visibility, which is what stops a
        // generated client from demanding a field the caller must not send.
        var spec = await ScenarioSpec.Play();

        Assert.That(spec, Does.Contain("@visibility(Lifecycle.Read) LastModified?: offsetDateTime;"));
    }

    [Test]
    public async Task Emit_ForARoleReachedFromAScenario_DeclaresItAsANamedModelWithOptionalityIntact()
    {
        // The Persist scenario plays IPersist, so Persist reaches the contract as a model of its
        // own rather than inlined -- which is what lets a recursive role graph terminate without
        // being truncated to an empty object.
        var spec = await ScenarioSpec.Play();

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
    public async Task Emit_ForADirectorScenario_UsesGetAndSendsNoBody()
    {
        var spec = await ScenarioSpec.Play();

        Assert.Multiple(() =>
        {
            Assert.That(spec, Does.Contain("@get"));
            // A director scenario takes no role input, so its operation has an empty parameter list.
            Assert.That(spec, Does.Contain("Scenarios(): Envelope<"));
            // ...and the director's own properties are injected services, never contract data.
            Assert.That(spec, Does.Not.Contain("model Director {"));
        });
    }

    [Test]
    public async Task Emit_PlayedTwice_ReturnsTheSameTextSoDriftIsDiffable()
    {
        var first = await ScenarioSpec.Play();
        var second = await ScenarioSpec.Play();

        Assert.That(second, Is.EqualTo(first));
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
}
