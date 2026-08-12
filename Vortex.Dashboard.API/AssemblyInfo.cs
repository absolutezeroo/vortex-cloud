using System.Runtime.CompilerServices;

// The dashboard's HTTP surface is internal by design (nothing outside the host composes it), but the
// request-validation filter is now the single place that enforces "no dashboard write without an
// audited reason" for every endpoint -- that rule is worth testing directly.
[assembly: InternalsVisibleTo("Vortex.Dashboard.Tests")]
