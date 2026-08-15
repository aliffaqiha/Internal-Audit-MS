using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IAMS.API.IntegrationTests;

public class RateLimitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RateLimitTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"))
            .CreateClient();
    }

    [Fact]
    public async Task Login_ReturnsTooManyRequests_AfterPermitLimit()
    {
        // Login policy is 10 requests/minute. First 10 must be processed (401/500, never 429).
        for (var i = 0; i < 10; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new { emailOrUsername = "unknown_user", password = "wrong-password" });

            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        // The 11th request in the same window must be rejected with 429.
        var rejected = await _client.PostAsJsonAsync("/api/auth/login",
            new { emailOrUsername = "unknown_user", password = "wrong-password" });

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    public async Task Health_IsNotRateLimited()
    {
        // /health explicitly opts out of rate limiting (monitoring must never be throttled).
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
