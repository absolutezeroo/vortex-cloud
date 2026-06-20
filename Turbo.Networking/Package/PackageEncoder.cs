using System;
using System.Buffers;
using Microsoft.Extensions.Logging;
using SuperSocket.ProtoBase;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Networking.Revisions;
using Turbo.Primitives.Packets;

namespace Turbo.Networking.Package;

public sealed class PackageEncoder(IRevisionManager revisionManager, ILogger<PackageEncoder> logger)
    : IPackageEncoder<OutgoingPackage>
{
    private readonly IRevisionManager _revisionManager = revisionManager;
    private readonly ILogger<PackageEncoder> _logger = logger;

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

                    _logger.LogDebug("Outgoing {Composer}", pack.Composer);

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
                }
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
        }

        return 0;
    }
}
