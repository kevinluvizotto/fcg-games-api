using System.Net;
using System.Net.Http.Json;
using FCG.Games.Api.Models;
using Xunit;

namespace FCG.Games.Tests;

public class GamesUpdateTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GamesUpdateTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Put_ShouldUpdateAndReturn200_WhenGameExists()
    {
        var token = TestJwtHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new GameCreateDto
        {
            Title = "FIFA 23",
            Genre = "Sports",
            Price = 249.99m,
            ReleaseDate = DateTime.UtcNow.AddYears(-1)
        };

        var createResponse = await _client.PostAsJsonAsync("/games", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<Game>();

        var updateDto = new GameUpdateDto
        {
            Title = "EA Sports FC 24",
            Genre = "Sports",
            Price = 299.99m,
            ReleaseDate = created!.ReleaseDate
        };

        var response = await _client.PutAsJsonAsync($"/games/{created.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<Game>();
        Assert.NotNull(updated);
        Assert.Equal("EA Sports FC 24", updated!.Title);
        Assert.Equal(299.99m, updated.Price);
    }
}
