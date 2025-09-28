using System.Net;
using System.Net.Http.Json;
using FCG.Games.Api.Models;
using Xunit;

namespace FCG.Games.Tests;

public class GamesCreateTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GamesCreateTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostGames_ShouldCreate_AndReturn201_WithGamePayload()
    {
        var token = TestJwtHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new GameCreateDto
        {
            Title = "Forza Horizon",
            Genre = "Racing",
            Price = 299.99m,
            ReleaseDate = DateTime.UtcNow.AddYears(-1)
        };

        var response = await _client.PostAsJsonAsync("/games", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Game>();
        Assert.NotNull(created);
        Assert.Equal("Forza Horizon", created!.Title);
    }
}
