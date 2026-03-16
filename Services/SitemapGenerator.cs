using Microsoft.AspNetCore.SignalR;
using VerSitemap.Hubs;
using VerSitemap.Models;

namespace VerSitemap.Services;

public sealed class SitemapGenerator
{
    private readonly ScrapeState _state;
    private readonly ScraperApiClient _apiClient;
    private readonly IHubContext<SitemapHub> _hubContext;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SitemapGenerator(
        ScrapeState state,
        ScraperApiClient apiClient,
        IHubContext<SitemapHub> hubContext)
    {
        _state = state;
        _apiClient = apiClient;
        _hubContext = hubContext;
    }

    public async Task StartAsync(IEnumerable<string> selectedUrls, CancellationToken cancellationToken)
    {
        if (!await _gate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            _state.SetRunning(true);
            await BroadcastStateAsync();

            var urls = _state.GetSelectedUrls(selectedUrls);
            var domain = _state.GetDomain();
            if (string.IsNullOrWhiteSpace(domain))
            {
                return;
            }

            foreach (var url in urls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _state.SetStatus(url, UrlCheckStatus.Checking);
                await BroadcastItemAsync(url);

                try
                {
                    var result = await _apiClient.PostCheckAsync(domain, url, cancellationToken);
                    _state.SetStatus(url, result.Ok ? UrlCheckStatus.Ok : UrlCheckStatus.Error, result.StatusCode);
                    _state.MarkProcessed(url);
                }
                catch (Exception ex)
                {
                    _state.SetStatus(url, UrlCheckStatus.Error, null, ex.Message);
                    _state.MarkProcessed(url);
                }

                await BroadcastItemAsync(url);
            }
        }
        finally
        {
            _state.SetRunning(false);
            await BroadcastStateAsync();
            _gate.Release();
        }
    }

    private Task BroadcastStateAsync()
    {
        return _hubContext.Clients.All.SendAsync("StateUpdated", _state.GetSnapshot());
    }

    private async Task BroadcastItemAsync(string url)
    {
        var item = _state.GetItem(url);
        if (item is not null)
        {
            await _hubContext.Clients.All.SendAsync("ItemUpdated", item);
        }
    }
}
