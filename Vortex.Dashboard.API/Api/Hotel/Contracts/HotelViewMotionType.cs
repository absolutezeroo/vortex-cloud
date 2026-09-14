using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// One of the four ways a background object can move, and the fields that steer it.
/// </summary>
/// <remarks>
/// The fields differ per motion and are positional, which is why a background object cannot be edited
/// as one free string without knowing its type first.
/// <para>
/// <paramref name="AssetPrefix"/> is not cosmetic: <c>randomwalk</c> resolves its sprite as
/// <c>${image.library.url}&lt;asset&gt;.png</c> while the other three resolve
/// <c>${image.library.url}reception/&lt;asset&gt;.png</c>. The same asset string therefore points at
/// two different files depending on the motion, and switching a working object from
/// <c>randomwalk</c> to <c>line</c> breaks its image with no error anywhere.
/// </para>
/// </remarks>
public sealed record HotelViewMotionType(
    string Motion,
    string AssetPrefix,
    IReadOnlyList<HotelViewArgument> Fields
);
