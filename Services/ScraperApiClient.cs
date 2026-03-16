using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace VerSitemap.Services;

public sealed class ScraperApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ScraperApiOptions _options;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt;

    public ScraperApiClient(HttpClient httpClient, IOptions<ScraperApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<string>> ScrapeAsync(string domain, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/getLinks");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { domain });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ScrapeResponse>(cancellationToken: cancellationToken);
        return payload?.Urls ?? new List<string>();
    }

    public async Task<PostCheckResult> PostCheckAsync(string domain, string url, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/postCheck");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { domain, url });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PostCheckResponse>(cancellationToken: cancellationToken);
        return new PostCheckResult(
            payload?.Url ?? url,
            payload?.Ok ?? false,
            payload?.StatusCode);
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
        {
            return _accessToken;
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = _options.Username,
            ["password"] = _options.Password
        });

        using var response = await _httpClient.PostAsync("/api/token", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        if (payload?.AccessToken is null)
        {
            throw new InvalidOperationException("No se pudo obtener el token de autenticacion.");
        }

        _accessToken = payload.AccessToken;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(50);
        return _accessToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }
    }

    private sealed class ScrapeResponse
    {
        public string? Domain { get; init; }
        [JsonPropertyName("total_urls")]
        public int TotalUrls { get; init; }
        [JsonPropertyName("urls")]
        public List<string> Urls { get; init; } = new();
    }

    private sealed class PostCheckResponse
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }
        [JsonPropertyName("ok")]
        public bool Ok { get; init; }
        [JsonPropertyName("status_code")]
        public int? StatusCode { get; init; }
    }
    public sealed record PostCheckResult(string Url, bool Ok, int? StatusCode);
}
