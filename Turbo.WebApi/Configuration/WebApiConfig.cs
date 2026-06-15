namespace Turbo.WebApi.Configuration;

public sealed class WebApiConfig
{
    public const string SECTION_NAME = "Turbo:WebApi";

    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
    public int MaxAvatarsPerAccount { get; set; } = 5;
    public string DefaultFigure { get; set; } = "hr-115-42.hd-195-19.ch-3030-82.lg-275-1408.fa-1201.ca-1804-64";
}
