using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Model;

namespace Vortex.Specs.Reasoning;

/// <summary>
/// Writes down what the sources do not answer.
/// </summary>
/// <remarks>
/// The list is the point of the whole exercise. An unknown that is recorded, with the evidence that
/// would close it, is a piece of work somebody can pick up; an unknown that was quietly filled in
/// with a plausible value is indistinguishable from knowledge and will never be revisited. Severity
/// ranks by what happens if it stays open: <see cref="UnknownSeverity.Critical"/> is reserved for
/// gaps somebody will otherwise fill with a guess.
/// </remarks>
public sealed class UnknownCollector
{
    public IReadOnlyList<UnknownSpec> Collect(
        SpecWorld world,
        IReadOnlyList<PacketSpec> packets,
        IReadOnlyList<FeatureSpec> features,
        IReadOnlyList<ScenarioSpec> scenarios
    )
    {
        List<UnknownSpec> unknowns = [];

        unknowns.AddRange(ClientPacketsWithNoHandler(world, packets));
        unknowns.AddRange(StubHandlers(features));
        unknowns.AddRange(UnsettledMutatingScenarios(features, scenarios));
        unknowns.AddRange(UnnamedFields(packets));
        unknowns.AddRange(UnknownRecipients(features));

        return
        [
            .. unknowns
                .OrderByDescending(u => u.Severity)
                .ThenBy(u => u.Subject, StringComparer.Ordinal)
                .ThenBy(u => u.Id, StringComparer.Ordinal),
        ];
    }

    /// <summary>
    /// A message the official client can send that this emulator has no parser for. The client
    /// proves the message exists, so this is not speculation — it is a hole with a known shape.
    /// </summary>
    private static IEnumerable<UnknownSpec> ClientPacketsWithNoHandler(
        SpecWorld world,
        IReadOnlyList<PacketSpec> packets
    )
    {
        HashSet<string> mapped =
        [
            .. packets
                .Where(p => p.Direction == PacketDirection.Incoming && p.MappedInVortex)
                .Select(p => p.Name),
        ];

        foreach (
            ClientScan client in world.Clients.Where(c =>
                c.Authority <= EvidenceAuthority.ClientCode
            )
        )
        {
            HashSet<int> knownIds = [.. world.Emulator.Registry.Incoming.Values];

            List<ClientPacket> orphans =
            [
                .. client
                    .Packets.Where(p =>
                        p.Direction == PacketDirection.Incoming
                        && p.HeaderId is not null
                        && !knownIds.Contains(p.HeaderId.Value)
                    )
                    .OrderBy(p => p.HeaderId),
            ];

            if (orphans.Count == 0)
            {
                continue;
            }

            yield return new UnknownSpec
            {
                Id = BuildId($"client-only-incoming:{client.Origin}"),
                Subject =
                    $"{client.Origin}: client-to-server messages this emulator has no header for",
                Question = string.Format(
                    CultureInfo.InvariantCulture,
                    "The official client can send {0} messages whose header ids appear nowhere in this "
                        + "emulator's table (for example {1}). What are they, and does the hotel need them?",
                    orphans.Count,
                    string.Join(
                        ", ",
                        orphans
                            .Take(8)
                            .Select(o => o.HeaderId!.Value.ToString(CultureInfo.InvariantCulture))
                    )
                ),
                Severity = UnknownSeverity.Critical,
                ResolvedBy =
                    "read the client class bound to each id in its message registry, then either map the "
                    + "header or record why the hotel does not need it",
                KnownEvidence = [.. orphans.Take(5).Select(o => o.Evidence)],
            };
        }

        // The reverse gap: a name this emulator maps that no same-revision client can produce.
        foreach (
            ClientScan client in world.Clients.Where(c =>
                c.TargetsSameRevision && c.Authority <= EvidenceAuthority.ClientCode
            )
        )
        {
            HashSet<int> clientIds =
            [
                .. client.Packets.Where(p => p.HeaderId is not null).Select(p => p.HeaderId!.Value),
            ];
            List<string> unreachable =
            [
                .. world
                    .Emulator.Registry.Incoming.Where(e => !clientIds.Contains(e.Value))
                    .Select(e => $"{e.Key}={e.Value.ToString(CultureInfo.InvariantCulture)}")
                    .OrderBy(e => e, StringComparer.Ordinal),
            ];

            if (unreachable.Count == 0)
            {
                continue;
            }

            yield return new UnknownSpec
            {
                Id = BuildId($"unreachable-headers:{client.Origin}"),
                Subject = "headers this emulator maps that the target client never sends",
                Question = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} incoming headers are registered here but appear in no composer of the target "
                        + "client build. Each is a handler that can never fire (for example {1}).",
                    unreachable.Count,
                    string.Join(", ", unreachable.Take(8))
                ),
                Severity = UnknownSeverity.Critical,
                ResolvedBy =
                    "check each id against the client's message registry; a header above the client's "
                    + "highest real id was invented and must be corrected or removed",
                KnownEvidence = [world.Emulator.Registry.Evidence],
            };
        }
    }

    private static IEnumerable<UnknownSpec> StubHandlers(IReadOnlyList<FeatureSpec> features)
    {
        List<FeatureSpec> stubs = [.. features.Where(f => !f.ObservedInVortex)];

        foreach (FeatureSpec feature in stubs.Take(200))
        {
            yield return new UnknownSpec
            {
                Id = BuildId($"stub:{feature.Id}"),
                Subject = feature.Id,
                Question =
                    "The handler for "
                    + string.Join(", ", feature.TriggerPackets)
                    + " reaches no domain operation and sends nothing. What is the message supposed to do?",
                Severity = UnknownSeverity.Critical,
                FeatureId = feature.Id,
                ResolvedBy =
                    "a capture of the official server answering this message, or the client code that "
                    + "consumes the reply",
                KnownEvidence = feature.Evidence,
            };
        }
    }

    private static IEnumerable<UnknownSpec> UnsettledMutatingScenarios(
        IReadOnlyList<FeatureSpec> features,
        IReadOnlyList<ScenarioSpec> scenarios
    )
    {
        foreach (
            FeatureSpec feature in features.Where(f =>
                f.Mutations.Count > 0 || f.ReachesPersistence
            )
        )
        {
            List<ScenarioSpec> open =
            [
                .. scenarios.Where(s =>
                    string.Equals(s.FeatureId, feature.Id, StringComparison.Ordinal)
                    && s.Expected == ScenarioOutcome.Unknown
                ),
            ];

            if (open.Count == 0)
            {
                continue;
            }

            yield return new UnknownSpec
            {
                Id = BuildId($"rejection-behaviour:{feature.Id}"),
                Subject = $"{feature.Id}: what the server sends when it refuses",
                Question = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} changes state and has {1} guards whose refusal behaviour nothing here attests. "
                        + "Does the official server answer a refused request, and with what?",
                    feature.Id,
                    open.Count
                ),
                Severity = UnknownSeverity.Medium,
                FeatureId = feature.Id,
                ResolvedBy = "an official capture of each guard being tripped",
                KnownEvidence = feature.Evidence,
            };
        }
    }

    private static IEnumerable<UnknownSpec> UnnamedFields(IReadOnlyList<PacketSpec> packets)
    {
        foreach (PacketSpec packet in packets)
        {
            List<PacketFieldSpec> unnamed = [.. packet.Fields.Where(f => f.IsPlaceholderName)];

            if (unnamed.Count == 0)
            {
                continue;
            }

            yield return new UnknownSpec
            {
                Id = BuildId($"field-names:{packet.SpecId}"),
                Subject = $"{packet.SpecId}: unnamed fields",
                Question = string.Format(
                    CultureInfo.InvariantCulture,
                    "Fields {0} carry no name from any source. What are they?",
                    string.Join(
                        ", ",
                        unnamed.Select(f => f.Index.ToString(CultureInfo.InvariantCulture))
                    )
                ),
                Severity = UnknownSeverity.Low,
                PacketName = packet.Name,
                ResolvedBy =
                    "a client getter or call site that names the value, or a decoded capture showing "
                    + "what it contains",
                KnownEvidence = [.. packet.Evidence.Take(3)],
            };
        }
    }

    private static IEnumerable<UnknownSpec> UnknownRecipients(IReadOnlyList<FeatureSpec> features)
    {
        foreach (FeatureSpec feature in features)
        {
            List<FeatureOutgoing> unknown =
            [
                .. feature.Outgoing.Where(o => o.Recipient == Recipient.Unknown),
            ];

            if (unknown.Count == 0)
            {
                continue;
            }

            yield return new UnknownSpec
            {
                Id = BuildId($"recipients:{feature.Id}"),
                Subject =
                    $"{feature.Id}: who receives {string.Join(", ", unknown.Select(o => o.Packet))}",
                Question =
                    "These composers are built on this feature's path but the send call that carries "
                    + "them could not be identified, so their audience is unknown. The recipient is "
                    + "part of the behaviour, not a detail.",
                Severity = UnknownSeverity.Medium,
                FeatureId = feature.Id,
                ResolvedBy =
                    "follow the composer to the send call in the emulator, or capture the same action "
                    + "from a second account in the room",
                KnownEvidence = [.. unknown.Select(o => o.Evidence).Take(3)],
            };
        }
    }

    public static string BuildId(string subject)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(subject));
        StringBuilder builder = new(16);
        builder.Append("uk_");

        for (int i = 0; i < 5; i++)
        {
            builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
