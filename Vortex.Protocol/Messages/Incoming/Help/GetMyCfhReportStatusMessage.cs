using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>
/// "Show me the reports I filed." Body-less, sent by HabboHelp::requestReportsStatus().
/// </summary>
public record GetMyCfhReportStatusMessage : IMessageEvent { }
