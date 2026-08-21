using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vortex.Specs.Naming;

/// <summary>
/// Reduces the four naming conventions in play to one symbolic name.
/// </summary>
/// <remarks>
/// The same message is <c>MoveObjectMessage</c> here, <c>MoveObjectMessageParser</c> in the revision
/// tree, <c>MoveObjectComposer</c> in Nitro and <c>MoveObjectMessageEvent</c> in Arcturus. Every
/// cross-source comparison in this system hangs off getting all four back to <c>MoveObject</c>, so
/// the rule lives in exactly one place and is tested against real names from all four trees.
/// </remarks>
public static class PacketNaming
{
    // Ordered longest first so the longest match wins: "ObjectUpdateMessageComposerSerializer" must
    // lose all of "MessageComposerSerializer", not just the "Serializer" tail.
    private static readonly string[] Suffixes =
    [
        "MessageComposerSerializer",
        "ComposerSerializer",
        "MessageComposer",
        "MessageHandler",
        "MessageParser",
        "MessageEvent",
        "Serializer",
        "Composer",
        "Handler",
        "Parser",
        "Message",
        "Event",
    ];

    /// <summary>
    /// Strips the implementation suffix and returns the symbolic name.
    /// </summary>
    /// <remarks>
    /// Exactly one suffix comes off, never a chain. Stripping repeatedly looks tidier and is wrong:
    /// <c>RoomEventMessage</c> would lose "Message" and then "Event" and land on <c>Room</c>,
    /// colliding with an unrelated packet. One pass keeps every real name in all four trees correct
    /// and leaves genuinely ambiguous names alone rather than mangling them.
    /// </remarks>
    public static string Canonical(string typeName)
    {
        foreach (string suffix in Suffixes)
        {
            if (
                typeName.Length > suffix.Length
                && typeName.EndsWith(suffix, StringComparison.Ordinal)
            )
            {
                return typeName[..^suffix.Length];
            }
        }

        return typeName;
    }

    /// <summary>
    /// The header-constant name for a canonical packet name in a given direction, as the Vortex
    /// <c>Headers.cs</c> spells it.
    /// </summary>
    public static IReadOnlyList<string> HeaderConstantCandidates(string canonical, bool incoming) =>
        incoming
            ? [canonical + "MessageEvent", canonical + "Event", canonical]
            : [canonical + "MessageComposer", canonical + "Composer", canonical];

    /// <summary>PascalCase or camelCase to snake_case, for field names in spec files.</summary>
    public static string SnakeCase(string name)
    {
        if (name.Length == 0)
        {
            return name;
        }

        StringBuilder builder = new(name.Length + 8);

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (c == '_')
            {
                if (builder.Length > 0 && builder[^1] != '_')
                {
                    builder.Append('_');
                }

                continue;
            }

            if (!char.IsUpper(c))
            {
                builder.Append(c);
                continue;
            }

            bool previousIsLower = i > 0 && char.IsLower(name[i - 1]);
            bool endsAnAcronym =
                i > 0
                && i + 1 < name.Length
                && char.IsUpper(name[i - 1])
                && char.IsLower(name[i + 1]);

            if ((previousIsLower || endsAnAcronym) && builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>
    /// True when a type name is a real symbol rather than something a decompiler or obfuscator made
    /// up. <c>_SafeCls_3667</c> and <c>class_165</c> carry no meaning and must never become the
    /// symbolic name of a packet; a class with a name like that is joined by header id or left out.
    /// </summary>
    public static bool IsSyntheticTypeName(string typeName)
    {
        foreach (string prefix in SyntheticPrefixes)
        {
            if (!typeName.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            string tail = typeName[prefix.Length..];

            if (tail.Length > 0 && tail.All(char.IsAsciiDigit))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly string[] SyntheticPrefixes =
    [
        "_SafeCls_",
        "_SafeStr_",
        "_SafePkg_",
        "_Str_",
        "_Cls_",
        "_Pkg_",
        "class_",
        "Class_",
    ];

    /// <summary>
    /// The domain a source tree's own folder layout puts a message in. Every tree here groups its
    /// messages under a direction folder and then a domain folder, so the segment after the
    /// direction is the domain — read from the tree rather than assigned by a taxonomy invented here.
    /// </summary>
    public static string DomainFromSourcePath(string? path)
    {
        if (path is null)
        {
            return "unsorted";
        }

        string[] parts = path.Split('/');

        for (int i = 0; i < parts.Length - 2; i++)
        {
            if (DirectionFolders.Contains(parts[i], StringComparer.OrdinalIgnoreCase))
            {
                return NormalizeDomain(parts[i + 1]);
            }
        }

        return "unsorted";
    }

    private static readonly string[] DirectionFolders =
    [
        "Parsers",
        "Serializers",
        "incoming",
        "outgoing",
        "parser",
    ];

    /// <summary>
    /// The domain folder a packet's spec goes in. Taken from the source tree's own folder layout —
    /// the revision maps and handler folders already group by domain, so no second taxonomy is
    /// invented here.
    /// </summary>
    public static string NormalizeDomain(string raw)
    {
        string domain = SnakeCase(raw);

        return domain switch
        {
            "" => "unsorted",
            "new_navigator" => "navigator",
            "room_directory" => "room",
            "room_settings" => "room",
            "friend_list" => "messenger",
            "friend_furni" => "messenger",
            "group_forums" => "groups",
            "user_defined_room_events" => "wired",
            "call_for_help" => "moderation",
            "moderator" => "moderation",
            "help" => "moderation",
            "users" => "users",
            "engine" => "room",
            "rooms" => "room",
            "items" => "room",
            "pets" => "room",
            "bots" => "room",
            "friends" => "messenger",
            "friendlist" => "messenger",
            "guilds" => "groups",
            "guildforums" => "groups",
            "modtool" => "moderation",
            "guides" => "moderation",
            "hotelview" => "landing_view",
            "gamecenter" => "game",
            "landingview" => "landing_view",
            "unknown" => "unsorted",
            "unknowns" => "unsorted",
            "generic" => "unsorted",
            _ => domain,
        };
    }
}
