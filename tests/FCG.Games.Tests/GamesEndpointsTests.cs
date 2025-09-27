using System.Net;
using System.Net.Http.Json;
using System.Collections.Generic;
using Xunit;

namespace FCG.Games.Tests;

public class GamesEndpointsTests
{
    [Fact]
    public async Task GetGames_ShouldReturnEmptyList_WhenNoGamesExist()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var games = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(games);
        Assert.Empty(games);
    }
}
