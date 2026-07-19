using System;
using Vortex.Primitives;

namespace Vortex.Logging;

public class VortexException : Exception
{
    public VortexErrorCodeEnum ErrorCode { get; }

    public VortexException(
        VortexErrorCodeEnum code,
        string? message = null,
        Exception? inner = null
    )
        : base(message ?? code.ToDefaultMessage(), inner)
    {
        ErrorCode = code;
    }
}
