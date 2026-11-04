using Backlot.Core;
using NUnit.Framework;

namespace Backlot.Testing;

[SetUpFixture]
public class TestAssemblySetup
{
    [OneTimeSetUp]
    public void Setup()
    {
        Loader.PreLoad();
    }
}