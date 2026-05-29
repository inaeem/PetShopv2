using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace PetShop.Tests.E2E;

/// <summary>
/// The API no longer issues tokens — every endpoint requires a valid, externally
/// issued JWT (validated with the shared signing key). These tests prove the bearer
/// requirement is enforced and that a token minted with the shared key is accepted.
/// </summary>
[Collection(ApiCollection.Name)]
public class AuthorizationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly PetShopApiFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationTests(PetShopApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPets_WithoutToken_Returns401Envelope()
    {
        var resp = await _client.GetAsync("/api/pets?page=1&pageSize=5");

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

    [Fact]
    public async Task GetPets_WithValidToken_ReturnsOk()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateToken("Admin"));

        var resp = await _client.GetAsync("/api/pets?page=1&pageSize=5");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
