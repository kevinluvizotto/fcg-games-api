using FCG.Games.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Games.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove o DbContext original
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GamesDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Adiciona um novo DbContext InMemory para testes
                services.AddDbContext<GamesDbContext>(options =>
                {
                    options.UseInMemoryDatabase("GamesTestDb");
                });
            });
        }
    }
}
