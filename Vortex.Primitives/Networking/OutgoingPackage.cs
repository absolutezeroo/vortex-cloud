namespace Vortex.Primitives.Networking;

public sealed record OutgoingPackage(ISessionContext Session, IComposer Composer);
