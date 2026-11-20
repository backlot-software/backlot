using Backlot.Core;

namespace Backlot.Testing.Shared.TestModels;

public interface IMoney : IRole
{
    decimal Value { get; set; }
    string Currency { get; set; }
}