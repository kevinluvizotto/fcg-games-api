using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using FCG.Games.Api.Models;

namespace FCG.Games.Api
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 🔒 Forçar uso do appsettings.json principal
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // --- Logging configuration ---
            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            });

            // --- Database configuration (Azure SQL) ---
            var connectionString = builder.Configuration.GetConnectionString("FCGDatabase")
                ?? throw new InvalidOperationException("Connection string 'FCGDatabase' não está configurada.");

            builder.Services.AddDbContext<GamesDbContext>(options =>
                options.UseSqlServer(connectionString));

            // --- JWT Authentication ---
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT Key não está configurada no appsettings.json.");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.NoResult();
                        context.Response.StatusCode = 401;
                        Console.WriteLine($"[JWT] Falha na autenticação: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        Console.WriteLine("[JWT] Token inválido ou ausente.");
                        return context.Response.WriteAsync("{\"error\": \"Token inválido ou ausente.\"}");
                    }
                };
            });

            // --- Authorization policies ---
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("Admin", "User"));
            });

            // --- Swagger ---
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Insira o token JWT sem o Bearer ou aspas",
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
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

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseAuthentication();
            app.UseAuthorization();

            // --- Health endpoint ---
            app.MapGet("/health", () =>
            {
                app.Logger.LogInformation("GET /health chamado.");
                return Results.Ok("OK");
            });

            // --- Debug endpoint: valida e decodifica token (assinatura real) ---
            app.MapGet("/debug/token", (HttpContext context, IConfiguration config) =>
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Results.BadRequest(new { error = "Token JWT não fornecido no header Authorization." });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();

                try
                {
                    var jwtKey = config["Jwt:Key"];
                    var issuer = config["Jwt:Issuer"];
                    var audience = config["Jwt:Audience"];

                    var handler = new JwtSecurityTokenHandler();

                    var validationParams = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
                        ClockSkew = TimeSpan.Zero
                    };

                    var principal = handler.ValidateToken(token, validationParams, out var validatedToken);
                    var jwt = (JwtSecurityToken)validatedToken;

                    var claims = principal.Claims.Select(c => new { c.Type, c.Value }).ToList();

                    return Results.Ok(new
                    {
                        message = "Token validado e decodificado com sucesso!",
                        issuer = jwt.Issuer,
                        audience = jwt.Audiences,
                        expiration = jwt.ValidTo,
                        claims
                    });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = $"Falha ao validar token: {ex.Message}" });
                }
            }).AllowAnonymous();

            // --- CRUD Endpoints /games ---
            app.MapPost("/games", async (Game game, GamesDbContext db, ILogger<Program> logger) =>
            {
                try
                {
                    logger.LogInformation("POST /games chamado com Title: {Title}, Genre: {Genre}, Price: {Price}",
                        game.Title, game.Genre, game.Price);

                    // 🔍 Validação de campos obrigatórios e regras de negócio
                    if (string.IsNullOrWhiteSpace(game.Title))
                        return Results.BadRequest(new { error = "O campo 'title' é obrigatório." });

                    if (string.IsNullOrWhiteSpace(game.Genre))
                        return Results.BadRequest(new { error = "O campo 'genre' é obrigatório." });

                    if (game.Price <= 0)
                        return Results.BadRequest(new { error = "O campo 'price' deve ser maior que zero." });

                    if (game.Description?.Length > 1000)
                        return Results.BadRequest(new { error = "A descrição não pode ultrapassar 1000 caracteres." });

                    // ✅ Se tudo ok, cria o jogo
                    game.Id = Guid.NewGuid();
                    db.Games.Add(game);
                    await db.SaveChangesAsync();

                    logger.LogInformation("Jogo criado com Id: {Id}", game.Id);
                    return Results.Created($"/games/{game.Id}", game);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao criar jogo.");
                    return Results.Json(
                        new { error = "Erro interno ao criar o jogo." },
                        statusCode: 500
                    );
                }
            }).RequireAuthorization("AdminOnly");

            app.MapGet("/games", async (GamesDbContext db, ILogger<Program> logger) =>
            {
                try
                {
                    logger.LogInformation("GET /games chamado.");
                    var games = await db.Games.ToListAsync();
                    logger.LogInformation("Retornados {Count} jogos.", games.Count);
                    return games.Any() ? Results.Ok(games) : Results.Ok(new List<Game>());
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao listar jogos.");
                    return Results.StatusCode(500);
                }
            }).RequireAuthorization("UserOrAdmin");

            app.MapGet("/games/{id}", async (Guid id, GamesDbContext db, ILogger<Program> logger) =>
            {
                try
                {
                    logger.LogInformation("GET /games/{Id} chamado.", id);
                    var game = await db.Games.FindAsync(id);
                    if (game == null)
                    {
                        logger.LogWarning("Jogo com Id {Id} não encontrado.", id);
                        return Results.NotFound();
                    }
                    logger.LogInformation("Jogo encontrado: {Id}", id);
                    return Results.Ok(game);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao obter jogo {Id}.", id);
                    return Results.StatusCode(500);
                }
            }).RequireAuthorization("UserOrAdmin");

            app.MapPut("/games/{id}", async (Guid id, Game updatedGame, GamesDbContext db, ILogger<Program> logger) =>
            {
                try
                {
                    logger.LogInformation("PUT /games/{Id} chamado com Title: {Title}, Genre: {Genre}, Price: {Price}",
                        id, updatedGame.Title, updatedGame.Genre, updatedGame.Price);
                    var game = await db.Games.FindAsync(id);
                    if (game == null)
                    {
                        logger.LogWarning("Jogo com Id {Id} não encontrado.", id);
                        return Results.NotFound();
                    }

                    game.Title = updatedGame.Title;
                    game.Genre = updatedGame.Genre;
                    game.Price = updatedGame.Price;
                    await db.SaveChangesAsync();
                    logger.LogInformation("Jogo com Id {Id} atualizado.", id);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao atualizar jogo {Id}.", id);
                    return Results.StatusCode(500);
                }
            }).RequireAuthorization("AdminOnly");

            app.MapDelete("/games/{id}", async (Guid id, GamesDbContext db, ILogger<Program> logger) =>
            {
                try
                {
                    logger.LogInformation("DELETE /games/{Id} chamado.", id);
                    var game = await db.Games.FindAsync(id);
                    if (game == null)
                    {
                        logger.LogWarning("Jogo com Id {Id} não encontrado.", id);
                        return Results.NotFound();
                    }

                    db.Games.Remove(game);
                    await db.SaveChangesAsync();
                    logger.LogInformation("Jogo com Id {Id} excluído.", id);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao excluir jogo {Id}.", id);
                    return Results.StatusCode(500);
                }
            }).RequireAuthorization("AdminOnly");

            app.Run();
        }
    }
}

// 👇 Necessário para testes e compatibilidade com WebApplicationFactory<Program>
public partial class Program { }