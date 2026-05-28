using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace PetShop.Tests.E2E;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public AuthEndpointsTests(PetShopApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Login_AsSeededAdmin_ReturnsToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = "Admin#12345" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var env = await resp.Content.ReadFromJsonAsync<ApiEnvelope<AuthData>>(Json);
        env!.Success.Should().BeTrue();
        env.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        env.Data.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Envelope()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = "wrong" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var env = await resp.Content.ReadFromJsonAsync<ApiEnvelope<object>>(Json);
        env!.Success.Should().BeFalse();
        env.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreatePet_WithoutToken_Returns401Envelope()
    {
        var resp = await _client.PostAsJsonAsync("/api/pets",
            new { name = "Rex", price = 100m, categoryId = 1 });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var env = await resp.Content.ReadFromJsonAsync<ApiEnvelope<object>>(Json);
        env!.Success.Should().BeFalse();
    }
}
