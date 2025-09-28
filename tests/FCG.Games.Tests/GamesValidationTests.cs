using System.Net;
using System.Net.Http.Json;
using FCG.Games.Api.Models;
using Xunit;

namespace FCG.Games.Tests;

public class GamesValidationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GamesValidationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("Fo", "Racing", 100)]
    [InlineData("Forza", "", 100)]
    [InlineData("Forza", "Ra", 100)]
    [InlineData("Forza", "Racing", -1)]
    public async Task Post_ShouldReturn400_WhenInvalid(string title, string genre, decimal price)
    {
        var token = TestJwtHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new GameCreateDto
        {
            Title = title,
            Genre = genre,
            Price = price,
            ReleaseDate = DateTime.UtcNow.AddYears(-1)
        };

        var response = await _client.PostAsJsonAsync("/games", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenReleaseDateInFuture()
    {
        var token = TestJwtHelper.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new GameCreateDto
        {
            Title = "Future Game",
            Genre = "Sci-Fi",
            Price = 59.99m,
            ReleaseDate = DateTime.UtcNow.AddYears(1)
        };

        var response = await _client.PostAsJsonAsync("/games", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
