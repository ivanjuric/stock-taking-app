using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockTakingApp.Data;
using StockTakingApp.Models.Enums;

namespace StockTakingApp.IntegrationTests;

public sealed class ThemeSwitchingTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task SetTheme_WhenAuthenticated_UpdatesUserThemePreference()
    {
        // Arrange
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // First, login as admin
        var loginPageResponse = await client.GetAsync("/account/login");
        var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginPageContent);

        var loginContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Email", "admin@demo.com"),
            new KeyValuePair<string, string>("Password", "Demo123!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
        ]);

        var loginResponse = await client.PostAsync("/account/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Get the authentication cookie
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        
        // Now get a page to extract a fresh anti-forgery token (after login)
        var homeResponse = await client.GetAsync("/");
        var homeContent = await homeResponse.Content.ReadAsStringAsync();
        var postLoginToken = ExtractAntiForgeryToken(homeContent);

        // Act - Set theme to Dark
        var setThemeContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("theme", "Dark"),
            new KeyValuePair<string, string>("__RequestVerificationToken", postLoginToken)
        ]);

        var setThemeResponse = await client.PostAsync("/account/settheme", setThemeContent);

        // Assert - Should redirect back
        setThemeResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Verify the theme was saved in the database
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await context.Users.FirstAsync(u => u.Email == "admin@demo.com");
        user.ThemePreference.Should().Be(ThemePreference.Dark);
    }

    [Fact]
    public async Task SetTheme_AfterChange_PageRendersWithCorrectDataThemeAttribute()
    {
        // Arrange
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Login as worker (different user to not conflict with other test)
        var loginPageResponse = await client.GetAsync("/account/login");
        var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginPageContent);

        var loginContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Email", "worker1@demo.com"),
            new KeyValuePair<string, string>("Password", "Demo123!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
        ]);

        await client.PostAsync("/account/login", loginContent);

        // Get home page and extract token
        var homeResponse = await client.GetAsync("/");
        var homeContent = await homeResponse.Content.ReadAsStringAsync();
        var postLoginToken = ExtractAntiForgeryToken(homeContent);

        // Act - Set theme to Light
        var setThemeContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("theme", "Light"),
            new KeyValuePair<string, string>("__RequestVerificationToken", postLoginToken)
        ]);

        var setThemeResponse = await client.PostAsync("/account/settheme", setThemeContent);
        
        // Follow the redirect
        var redirectLocation = setThemeResponse.Headers.Location;
        var finalPageResponse = await client.GetAsync(redirectLocation);
        var finalPageContent = await finalPageResponse.Content.ReadAsStringAsync();

        // Assert - The HTML should have data-theme="light"
        finalPageContent.Should().Contain("data-theme=\"light\"");
    }

    [Fact]
    public async Task SetTheme_ToSystem_PageRendersWithoutDataThemeAttribute()
    {
        // Arrange
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Login as worker2
        var loginPageResponse = await client.GetAsync("/account/login");
        var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
        var antiForgeryToken = ExtractAntiForgeryToken(loginPageContent);

        var loginContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Email", "worker2@demo.com"),
            new KeyValuePair<string, string>("Password", "Demo123!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken)
        ]);

        await client.PostAsync("/account/login", loginContent);

        // Get home page and extract token
        var homeResponse = await client.GetAsync("/");
        var homeContent = await homeResponse.Content.ReadAsStringAsync();
        var postLoginToken = ExtractAntiForgeryToken(homeContent);

        // First set to Dark
        var setDarkContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("theme", "Dark"),
            new KeyValuePair<string, string>("__RequestVerificationToken", postLoginToken)
        ]);
        await client.PostAsync("/account/settheme", setDarkContent);

        // Get new token
        var afterDarkResponse = await client.GetAsync("/");
        var afterDarkContent = await afterDarkResponse.Content.ReadAsStringAsync();
        var afterDarkToken = ExtractAntiForgeryToken(afterDarkContent);

        // Act - Set theme to System
        var setSystemContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("theme", "System"),
            new KeyValuePair<string, string>("__RequestVerificationToken", afterDarkToken)
        ]);

        var setThemeResponse = await client.PostAsync("/account/settheme", setSystemContent);
        
        // Follow the redirect
        var redirectLocation = setThemeResponse.Headers.Location;
        var finalPageResponse = await client.GetAsync(redirectLocation);
        var finalPageContent = await finalPageResponse.Content.ReadAsStringAsync();

        // Assert - The HTML should NOT have data-theme attribute (system uses CSS prefers-color-scheme)
        finalPageContent.Should().NotContain("data-theme=\"light\"");
        finalPageContent.Should().NotContain("data-theme=\"dark\"");
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        // Try the standard format first
        const string startMarker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var startIndex = html.IndexOf(startMarker);
        
        if (startIndex == -1)
        {
            // Try alternate format (type before name)
            const string altMarker = "type=\"hidden\" name=\"__RequestVerificationToken\" value=\"";
            startIndex = html.IndexOf(altMarker);
            if (startIndex != -1)
            {
                startIndex += altMarker.Length;
            }
        }
        else
        {
            startIndex += startMarker.Length;
        }

        if (startIndex == -1) return string.Empty;
        
        var endIndex = html.IndexOf("\"", startIndex);
        return html[startIndex..endIndex];
    }
}
