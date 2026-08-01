using System;
using System.Buffers;
using Microsoft.Extensions.Logging;
using SuperSocket.ProtoBase;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Packets;

namespace Vortex.Networking.Package;

public sealed class PackageEncoder(
    IRevisionManager revisionManager,
    ILogger<PackageEncoder> logger,
    IVortexMetrics metrics
) : IPackageEncoder<OutgoingPackage>
{
    private readonly IRevisionManager _revisionManager = revisionManager;
    private readonly ILogger<PackageEncoder> _logger = logger;
    private readonly IVortexMetrics _metrics = metrics;

    public int Encode(IBufferWriter<byte> writer, OutgoingPackage pack)
    {
        try
        {
            IRevision? revision = _revisionManager.GetRevision(pack.Session.RevisionId);

            if (revision is not null)
            {
                Type composerType = pack.Composer.GetType();

                if (revision.Serializers.TryGetValue(composerType, out ISerializer? serializer))
                {
                    byte[] payload = serializer.Serialize(pack.Composer).ToArray();

                    if (pack.Session.CryptoOut is not null)
                    {
                        payload = pack.Session.CryptoOut.Process(payload);
                    }

                    _logger.LogDebug("Outgoing {ComposerType}", composerType.Name);

                    writer.Write(payload);

                    return payload.Length;
                }
                else
                {
                    _logger.LogWarning(
                        "Serializer not found for {Name} for {SessionKey}",
                        composerType.Name,
                        pack.Session.SessionKey
                    );

                    // The packet silently vanishes here: SuperSocket writes nothing when Encode
                    // returns 0, so this counter is the only trace that it ever happened.
                    _metrics.PacketDropped("serializer_not_found");
                }
            }
            else
            {
                _metrics.PacketDropped("revision_not_found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to serialize packet {Packet} for session {SessionKey}",
                pack.Composer.GetType().Name,
                pack.Session.SessionKey
            );

            _metrics.PacketDropped("serialize_exception");
        }

        return 0;
    }
}
