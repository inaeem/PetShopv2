using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetShop.Data.Context;
using PetShop.Domain.Entities;
using Xunit;

namespace PetShop.Tests.E2E;

[Collection(ApiCollection.Name)]
public class PetEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly PetShopApiFactory _factory;
    private readonly HttpClient _client;

    public PetEndpointsTests(PetShopApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePet_ThenList_ReturnsCreatedPet()
    {
        var categoryId = await EnsureCategoryAsync("Dogs");
        Authenticate();

        var create = await _client.PostAsJsonAsync("/api/pets",
            new { name = "Rex", breed = "German Shepherd", price = 650m, ageMonths = 8, categoryId });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ApiEnvelope<PetData>>(Json);
        created!.Data!.Id.Should().BeGreaterThan(0);

        var list = await _client.GetFromJsonAsync<ApiEnvelope<PagedData<PetData>>>("/api/pets?page=1&pageSize=50", Json);
        list!.Data!.Items.Should().Contain(p => p.Name == "Rex");
    }

    [Fact]
    public async Task CreatePet_WithInvalidPayload_Returns400WithErrors()
    {
        Authenticate();

        // Empty name + negative price + missing category → fails FluentValidation.
        var resp = await _client.PostAsJsonAsync("/api/pets",
            new { name = "", price = -5m, categoryId = 0 });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var env = await resp.Content.ReadFromJsonAsync<ApiEnvelope<object>>(Json);
        env!.Success.Should().BeFalse();
        env.Errors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchPets_UsesStoredProcedure_ReturnsOk()
    {
        var categoryId = await EnsureCategoryAsync("Cats");
        Authenticate();
        await _client.PostAsJsonAsync("/api/pets",
            new { name = "Whiskers", breed = "Siamese", price = 300m, categoryId });

        // Exercises dbo.usp_SearchPets end-to-end (proves the proc migration ran).
        var resp = await _client.GetAsync("/api/pets/search?term=Whisk");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var env = await resp.Content.ReadFromJsonAsync<ApiEnvelope<PetData[]>>(Json);
        env!.Success.Should().BeTrue();
        env.Data.Should().Contain(p => p.Name == "Whiskers");
    }

    // --- helpers ---

    // Tokens are minted locally (signed with the shared key) — the API no longer
    // issues them. Admin role lets these tests hit role-restricted endpoints too.
    private void Authenticate()
        => _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateToken("Admin"));

    private async Task<int> EnsureCategoryAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PetShopDbContext>();
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (existing is not null) return existing.Id;

        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }
}
