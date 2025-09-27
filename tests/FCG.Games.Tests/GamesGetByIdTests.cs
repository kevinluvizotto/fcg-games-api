using System.Net;
using System.Net.Http.Json;
using FCG.Games.Api.Models;
using Xunit;

namespace FCG.Games.Tests;

public class GamesGetByIdTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GamesGetByIdTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGameById_ShouldReturnGame_WhenGameExists()
    {
        // Precisa criar jogo com token Admin
        var token = TestJwtHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new GameCreateDto
        {
            Title = "Halo Infinite",
            Genre = "Shooter",
            Price = 199.99m,
            ReleaseDate = DateTime.UtcNow.AddYears(-2)
        };

        var createResponse = await _client.PostAsJsonAsync("/games", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Game>();

        // Limpa header para simular GET público
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/games/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var game = await response.Content.ReadFromJsonAsync<Game>();
        Assert.NotNull(game);
        Assert.Equal("Halo Infinite", game!.Title);
    }
}
