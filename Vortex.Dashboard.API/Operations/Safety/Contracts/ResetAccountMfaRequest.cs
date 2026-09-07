using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations.Safety.Contracts;

/// <summary>
/// Clears another operator's second factor. This is the recovery path for a lost authenticator --
/// there are no one-time codes to keep in a drawer -- which makes it a way to strip a factor off an
/// account, so it lives behind OpsStaffManage and carries a reason like every other staff write.
/// </summary>
public sealed record ResetAccountMfaRequest(int AccountId, string Reason) : IReasonedRequest;
