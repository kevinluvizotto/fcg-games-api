using FCG.Games.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Games.Tests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove o contexto original
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GamesDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Adiciona banco em memória isolado para testes
                services.AddDbContext<GamesDbContext>(options =>
                {
                    options.UseInMemoryDatabase("GamesTestDb");
                });

                // Garante banco limpo antes de cada teste
                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GamesDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            });
        }
    }
}
