using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Turbo.Observability.ErrorTracking;

internal sealed record ErrorGroupingRecord(
    string Fingerprint,
    string Source,
    string Operation,
    string ExceptionType,
    string MessageSignature,
    string Message,
    string? StackTrace,
    DateTime OccurredAt,
    string? CorrelationId,
    long? ActorPlayerId,
    int? RoomId,
    string? SessionKey,
    string? RemoteIp
)
{
    public static ErrorGroupingRecord FromException(
        Exception exception,
        string source,
        string operation,
        DateTime occurredAt,
        long? actorId = null,
        int? roomId = null,
        string? correlationId = null,
        string? sessionKey = null,
        string? remoteIp = null
    )
    {
        Exception root = GetRootException(exception);
        string exceptionType = root.GetType().FullName ?? root.GetType().Name;
        string message = Normalize(root.Message);
        string messageSignature = message.Length <= 120 ? message : message[..120];
        string stackTrace = Normalize(root.StackTrace);
        string? topStackLine = stackTrace
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        string fingerprint = ComputeFingerprint(
            source,
            operation,
            exceptionType,
            messageSignature,
            topStackLine
        );

        return new ErrorGroupingRecord(
            fingerprint,
            source,
            operation,
            exceptionType,
            messageSignature,
            message,
            string.IsNullOrWhiteSpace(stackTrace) ? null : stackTrace,
            occurredAt,
            correlationId,
            actorId,
            roomId,
            sessionKey,
            remoteIp
        );
    }

    private static Exception GetRootException(Exception ex)
    {
        Exception? current = ex;

        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            value.ReplaceLineEndings(" ").Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );
    }

    private static string ComputeFingerprint(
        string source,
        string operation,
        string exceptionType,
        string messageSignature,
        string? topStackLine
    )
    {
        StringBuilder builder = new(256);
        builder.Append(source);
        builder.Append('|');
        builder.Append(operation);
        builder.Append('|');
        builder.Append(exceptionType);
        builder.Append('|');
        builder.Append(messageSignature);
        builder.Append('|');
        builder.Append(topStackLine);

        byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
