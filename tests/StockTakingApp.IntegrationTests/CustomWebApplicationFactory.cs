using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockTakingApp.Data;
using StockTakingApp.Services;

namespace StockTakingApp.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"IntegrationTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Find and remove all services related to DbContext
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                            d.ServiceType == typeof(DbContextOptions) ||
                            d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Also need to remove the DbContext itself since it captures the options
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(AppDbContext));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Clear all EntityFramework related services more aggressively  
            var efDescriptors = services.Where(s => 
                s.ServiceType.Assembly.FullName?.Contains("EntityFrameworkCore") == true ||
                s.ImplementationType?.Assembly.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in efDescriptors)
            {
                services.Remove(d);
            }

            // Add fresh DbContext with in-memory database
            services.AddDbContext<AppDbContext>((_, options) =>
            {
                options.UseInMemoryDatabase(_databaseName);
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        });

        builder.ConfigureServices(services =>
        {
            // Build the service provider and seed data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            var authService = scopedServices.GetRequiredService<IAuthService>();

            db.Database.EnsureCreated();
            DbSeeder.SeedAsync(db, authService).GetAwaiter().GetResult();
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Get login page to extract anti-forgery token
        var loginPageResponse = await client.GetAsync("/account/login");
        var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
        
        var requestVerificationToken = ExtractAntiForgeryToken(loginPageContent);
        
        // Submit login form
        var loginContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", requestVerificationToken)
        ]);

        var loginResponse = await client.PostAsync("/account/login", loginContent);

        // Get cookies and create new client with cookies
        if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var cookieContainer = new System.Net.CookieContainer();
            foreach (var cookie in cookies)
            {
                // Parse and add cookies
            }
        }

        // Return client with AllowAutoRedirect true for subsequent requests
        return CreateClient();
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        // Simple extraction - in real world you'd use HtmlAgilityPack
        const string startMarker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var startIndex = html.IndexOf(startMarker);
        if (startIndex == -1) return string.Empty;
        
        startIndex += startMarker.Length;
        var endIndex = html.IndexOf("\"", startIndex);
        return html[startIndex..endIndex];
    }
}
