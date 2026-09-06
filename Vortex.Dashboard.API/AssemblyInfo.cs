using System.Runtime.CompilerServices;

// The dashboard's HTTP surface is internal by design (nothing outside the host composes it), but the
// request-validation filter is now the single place that enforces "no dashboard write without an
// audited reason" for every endpoint -- that rule is worth testing directly.
[assembly: InternalsVisibleTo("Vortex.Dashboard.Tests")]
// The reward-track content rules moved here with the admin service that uses them, but their tests
// are built on Vortex.Rewards.Tests' own snapshot builder, which reaches reward-track domain types
// this project deliberately does not reference.
[assembly: InternalsVisibleTo("Vortex.Rewards.Tests")]
// Same shape: the poll and quest authoring rules moved here with their admin services, but their
// tests reach the poll grain and this project's own fakes, which the dashboard does not reference.
[assembly: InternalsVisibleTo("Vortex.Players.Tests")]
