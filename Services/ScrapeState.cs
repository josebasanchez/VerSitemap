using VerSitemap.Models;

namespace VerSitemap.Services;

public sealed class ScrapeState
{
    private readonly object _lock = new();
    private string? _domain;
    private List<UrlItem> _items = new();
    private bool _isRunning;

    public ScrapeSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new ScrapeSnapshot
            {
                Domain = _domain,
                IsRunning = _isRunning,
                Items = _items.Select(ToDto).ToList()
            };
        }
    }

    public string? GetDomain()
    {
        lock (_lock)
        {
            return _domain;
        }
    }

    public UrlItemDto? GetItem(string url)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));
            return item is null ? null : ToDto(item);
        }
    }

    public void SetUrls(string domain, IEnumerable<string> urls)
    {
        var list = urls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
            .Select(u => new UrlItem
            {
                Url = u.Trim(),
                Selected = false,
                Status = UrlCheckStatus.Idle
            })
            .ToList();

        lock (_lock)
        {
            _domain = domain;
            _items = list;
            _isRunning = false;
        }
    }

    public void UpdateSelection(IEnumerable<string> selectedUrls)
    {
        var selected = new HashSet<string>(selectedUrls, StringComparer.OrdinalIgnoreCase);
        lock (_lock)
        {
            foreach (var item in _items)
            {
                item.Selected = selected.Contains(item.Url);
                if (!item.Selected)
                {
                    if (item.Status == UrlCheckStatus.Checking)
                    {
                        item.Status = UrlCheckStatus.Idle;
                        item.StatusCode = null;
                        item.Error = null;
                    }
                }
            }
        }
    }

    public void SetRunning(bool isRunning)
    {
        lock (_lock)
        {
            _isRunning = isRunning;
        }
    }

    public void SetStatus(string url, UrlCheckStatus status, int? statusCode = null, string? error = null)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));
            if (item is null)
            {
                return;
            }

            item.Status = status;
            item.StatusCode = statusCode;
            item.Error = error;
        }
    }

    public void MarkProcessed(string url)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));
            if (item is null)
            {
                return;
            }

            item.Selected = false;
        }
    }

    public IReadOnlyList<string> GetSelectedUrls(IEnumerable<string> requested)
    {
        var requestedSet = new HashSet<string>(requested, StringComparer.OrdinalIgnoreCase);
        lock (_lock)
        {
            return _items
                .Where(i => i.Selected && requestedSet.Contains(i.Url))
                .Select(i => i.Url)
                .ToList();
        }
    }

    private static UrlItemDto ToDto(UrlItem item)
    {
        string? detail = null;
        if (item.Status == UrlCheckStatus.Error)
        {
            if (item.StatusCode is not null)
            {
                detail = item.StatusCode.Value.ToString();
            }
            else if (!string.IsNullOrWhiteSpace(item.Error))
            {
                detail = item.Error;
            }
        }

        return new UrlItemDto
        {
            Url = item.Url,
            Selected = item.Selected,
            Status = item.Status,
            StatusDetail = detail
        };
    }
}
