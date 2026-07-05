namespace Turbo.Primitives.Moderation;

/// <summary>Numeric values match the WIN63 client's own class_3291 STATE_OPEN/const_951/const_585
/// constants exactly, since this is written directly as the wire "state" field in the moderator
/// issue queue — no translation layer needed between the DB value and what the client expects.</summary>
public enum CfhTicketState
{
    Open = 1,
    Picked = 2,
    Closed = 3,
}
