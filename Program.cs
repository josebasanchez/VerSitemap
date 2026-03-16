using Microsoft.Extensions.Options;
using VerSitemap.Hubs;
using VerSitemap.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.Configure<ScraperApiOptions>(builder.Configuration.GetSection("ScraperApi"));
builder.Services.AddHttpClient<ScraperApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ScraperApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddSingleton<ScrapeState>();
builder.Services.AddSingleton<SitemapGenerator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHub<SitemapHub>("/sitemapHub");

app.Run();
