using Backlot.Core;
using Backlot.Defaults.Instructing;

namespace Backlot.Testing.Core;

[FieldInfoAlias(nameof(Uid), ["BSN AN"])]
public interface IPerson : IPersist
{
}