using System.Net;
using System.Net.Http.Json;
using FCG.Games.Api.Models;
using Xunit;

namespace FCG.Games.Tests;

public class GamesDeleteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GamesDeleteTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenGameExists()
    {
        var token = TestJwtHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new GameCreateDto
        {
            Title = "Cyberpunk 2077",
            Genre = "RPG",
            Price = 199.99m,
            ReleaseDate = DateTime.UtcNow.AddYears(-3)
        };

        var createResponse = await _client.PostAsJsonAsync("/games", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Game>();

        var response = await _client.DeleteAsync($"/games/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
