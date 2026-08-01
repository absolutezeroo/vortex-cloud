namespace Vortex.Primitives.Players.Enums;

/// <summary>
/// Result codes for the name check/claim exchange.
/// </summary>
/// <remarks>
/// The values are the client's, read off the 701 dump
/// (<c>WIN63-202607011411-782849652/src/unknowns/_SafePkg_1759/_SafeCls_2167.as</c>): the client
/// switches on them directly to pick which message it shows, and treats anything it does not
/// recognise as no message at all.
/// </remarks>
public enum NameChangeResultCode
{
    Ok = 0,
    NameRequired = 1,
    NameTooShort = 2,
    NameTooLong = 3,
    NameNotValid = 4,
    NameInUse = 5,
    ChangeNotAllowed = 6,
    MergeHotelDown = 7,
}
