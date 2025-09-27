namespace FCG.Games.Tests;

// DTOs usados apenas nos testes
public record CreateGameRequest(string Title, string Genre, decimal Price, DateTime? ReleaseDate);
public record UpdateGameRequest(string Title, string Genre, decimal Price, DateTime? ReleaseDate);
public record GameResponse(Guid Id, string Title, string Genre, decimal Price, DateTime ReleaseDate);
