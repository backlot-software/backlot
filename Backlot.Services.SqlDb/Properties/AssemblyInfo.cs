using System.Runtime.CompilerServices;

// The query building seams of the role repository are internal: they are an implementation detail
// of this provider and not part of the published api. Backlot.Testing asserts the query they
// generate matches the criteria group semantics documented in AGENTS.MD.
[assembly: InternalsVisibleTo("Backlot.Testing")]
