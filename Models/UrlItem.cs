namespace VerSitemap.Models;

public sealed class UrlItem
{
    public string Url { get; init; } = string.Empty;
    public bool Selected { get; set; }
    public UrlCheckStatus Status { get; set; } = UrlCheckStatus.Idle;
    public int? StatusCode { get; set; }
    public string? Error { get; set; }
}
