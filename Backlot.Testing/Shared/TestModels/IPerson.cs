using Backlot.Core;
using Backlot.Defaults.Instructing;

namespace Backlot.Testing.Shared.TestModels;

[FieldInfoAlias(nameof(Uid), ["BSN AN"])]
public interface IPerson : IPersist
{
}