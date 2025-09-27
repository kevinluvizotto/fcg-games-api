using FCG.Games.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FCG.Games.Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// EF Core InMemory
builder.Services.AddDbContext<GamesDbContext>(opt =>
    opt.UseInMemoryDatabase("GamesDb"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// GET /games -> lista todos
app.MapGet("/games", async (GamesDbContext db) =>
    await db.Games.AsNoTracking().ToListAsync())
.WithName("GetGames")
.WithTags("Games");

// POST /games -> cria jogo
app.MapPost("/games", async (GameCreateDto dto, GamesDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 3)
        return Results.BadRequest("Title is required and must be at least 3 characters long.");
    if (string.IsNullOrWhiteSpace(dto.Genre) || dto.Genre.Length < 3)
        return Results.BadRequest("Genre is required and must be at least 3 characters long.");
    if (dto.Price < 0)
        return Results.BadRequest("Price must be >= 0.");
    if (dto.ReleaseDate.HasValue && dto.ReleaseDate.Value.Date > DateTime.UtcNow.Date)
        return Results.BadRequest("Release date cannot be in the future.");

    var game = new Game
    {
        Title = dto.Title.Trim(),
        Genre = dto.Genre.Trim(),
        Price = dto.Price,
        ReleaseDate = dto.ReleaseDate?.Date ?? DateTime.UtcNow.Date
    };

    db.Games.Add(game);
    await db.SaveChangesAsync();

    return Results.Created($"/games/{game.Id}", game);
})
.RequireAuthorization("AdminOnly")
.WithName("CreateGame")
.WithTags("Games");

// GET /games/{id} -> busca jogo por Id
app.MapGet("/games/{id:guid}", async (Guid id, GamesDbContext db) =>
{
    var game = await db.Games
        .AsNoTracking()
        .FirstOrDefaultAsync(g => g.Id == id);

    return game is not null
        ? Results.Ok(game)
        : Results.NotFound();
})
.WithName("GetGameById")
.WithTags("Games");

// PUT /games/{id} -> atualiza jogo
app.MapPut("/games/{id:guid}", async (Guid id, GameUpdateDto dto, GamesDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 3)
        return Results.BadRequest("Title is required and must be at least 3 characters long.");
    if (string.IsNullOrWhiteSpace(dto.Genre) || dto.Genre.Length < 3)
        return Results.BadRequest("Genre is required and must be at least 3 characters long.");
    if (dto.Price < 0)
        return Results.BadRequest("Price must be >= 0.");
    if (dto.ReleaseDate.HasValue && dto.ReleaseDate.Value.Date > DateTime.UtcNow.Date)
        return Results.BadRequest("Release date cannot be in the future.");

    var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id);
    if (game is null)
        return Results.NotFound();

    game.Title = dto.Title.Trim();
    game.Genre = dto.Genre.Trim();
    game.Price = dto.Price;
    game.ReleaseDate = dto.ReleaseDate?.Date ?? game.ReleaseDate;

    await db.SaveChangesAsync();

    return Results.Ok(game);
})
.RequireAuthorization("AdminOnly")
.WithName("UpdateGame")
.WithTags("Games");

// DELETE /games/{id} -> remove jogo
app.MapDelete("/games/{id:guid}", async (Guid id, GamesDbContext db) =>
{
    var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id);
    if (game is null)
        return Results.NotFound();

    db.Games.Remove(game);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization("AdminOnly")
.WithName("DeleteGame")
.WithTags("Games");

app.Run();

// Necessário para WebApplicationFactory nos testes
public partial class Program { }
