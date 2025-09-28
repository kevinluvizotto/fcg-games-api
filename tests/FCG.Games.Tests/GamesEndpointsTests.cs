using System.Net;
using System.Net.Http.Json;
using FCG.Games.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FCG.Games.Tests
{
    public class GamesEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public GamesEndpointsTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetGames_ShouldReturnEmptyList_WhenNoGamesExist()
        {
            // Limpa o banco ANTES de rodar o teste
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
                db.Games.RemoveRange(db.Games);
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/games");

            // Assert
            response.EnsureSuccessStatusCode();
            var games = await response.Content.ReadFromJsonAsync<List<Game>>();
            Assert.NotNull(games);
            Assert.Empty(games!);
        }
    }
}
