namespace VerSitemap.Services;

public sealed class ScraperApiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
}
