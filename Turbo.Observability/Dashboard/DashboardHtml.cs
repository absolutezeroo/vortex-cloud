namespace Turbo.Observability.Dashboard;

internal static class DashboardHtml
{
    public static byte[] PageBytes => DashboardPageResources.PageBytes;

    public static byte[] CssBytes => DashboardPageResources.CssBytes;
    public static byte[] ScriptBytes => DashboardPageResources.ScriptBytes;
}
