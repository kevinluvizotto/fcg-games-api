# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore fcg-games-api.sln
RUN dotnet publish src/FCG.Games.Api/FCG.Games.Api.csproj -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "FCG.Games.Api.dll"]
