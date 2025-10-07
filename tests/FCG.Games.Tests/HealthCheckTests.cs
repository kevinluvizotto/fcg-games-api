using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FCG.Games.Tests
{
    public class HealthCheckTests : IClassFixture<WebApplicationFactory<FCG.Games.Api.Program>>
    {
        private readonly WebApplicationFactory<FCG.Games.Api.Program> _factory;

        public HealthCheckTests(WebApplicationFactory<FCG.Games.Api.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Health_ShouldReturn_OK()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
