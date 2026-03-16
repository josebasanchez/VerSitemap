using System.Linq;

namespace VerSitemap.Models;

public sealed class ScrapeSnapshot
{
    public string? Domain { get; init; }
    public bool IsRunning { get; init; }
    public IReadOnlyList<UrlItemDto> Items { get; init; } = Array.Empty<UrlItemDto>();
    public int Total => Items.Count;
    public int Completed => Items.Count(item => item.Status is UrlCheckStatus.Ok or UrlCheckStatus.Error);
}

public sealed class UrlItemDto
{
    public string Url { get; init; } = string.Empty;
    public bool Selected { get; init; }
    public UrlCheckStatus Status { get; init; }
    public string? StatusDetail { get; init; }
}
