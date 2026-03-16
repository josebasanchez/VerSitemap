using Microsoft.AspNetCore.SignalR;
using VerSitemap.Services;

namespace VerSitemap.Hubs;

public sealed class SitemapHub : Hub
{
    private readonly ScraperApiClient _apiClient;
    private readonly ScrapeState _state;
    private readonly SitemapGenerator _generator;

    public SitemapHub(ScraperApiClient apiClient, ScrapeState state, SitemapGenerator generator)
    {
        _apiClient = apiClient;
        _state = state;
        _generator = generator;
    }

    public Task GetState()
    {
        return Clients.Caller.SendAsync("StateUpdated", _state.GetSnapshot());
    }

    public async Task LoadDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return;
        }

        var urls = await _apiClient.ScrapeAsync(domain.Trim(), Context.ConnectionAborted);
        _state.SetUrls(domain.Trim(), urls);
        await Clients.All.SendAsync("StateUpdated", _state.GetSnapshot());
    }

    public async Task StartGenerate(string[] selectedUrls)
    {
        _state.UpdateSelection(selectedUrls);
        await Clients.All.SendAsync("StateUpdated", _state.GetSnapshot());
        _ = _generator.StartAsync(selectedUrls, Context.ConnectionAborted);
    }
}
