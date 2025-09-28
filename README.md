# 🎮 FCG Games API

API de **Jogos** da plataforma **FIAP Cloud Games (FCG)**.  
Parte da **Fase 3** (migração para microsserviços).

---

## 🚀 Tecnologias
- .NET 8 (Minimal APIs)
- Entity Framework Core (InMemory para dev/testes)
- JWT Authentication (JSON Web Token)
- Roles (User/Admin)
- Swagger (documentação e testes de endpoints)
- xUnit (testes automatizados)
- Docker

---

## 📦 Como rodar localmente

### Pré-requisitos
- .NET 8 SDK  
- (Opcional) Docker

### Passos
```bash
# Restaurar pacotes e compilar
dotnet build fcg-games-api.sln

# Rodar a API
dotnet run --project src/FCG.Games.Api
```

A aplicação sobe em:  
👉 http://localhost:5193  

Swagger UI disponível em:  
👉 http://localhost:5193/swagger  

---

## 🔑 Autenticação JWT

Todos os endpoints de criação, atualização e remoção de jogos exigem **token JWT** com role **Admin**.  

### Exemplo de payload de login (feito pelo `fcg-users-api`)
```json
{
  "email": "admin@fcg.com",
  "password": "123456"
}
```

Resposta:
```json
{
  "token": "<seu-jwt-token>"
}
```

Usar no **header**:
```
Authorization: Bearer <seu-jwt-token>
```

---

## 📌 Endpoints

### GET /games
Lista todos os jogos.

### GET /games/{id}
Busca jogo por Id.

### POST /games  🔒 *(Admin Only)*
Cria um novo jogo.  
Body:
```json
{
  "title": "Forza Horizon 5",
  "genre": "Racing",
  "price": 299.90,
  "releaseDate": "2023-10-10"
}
```

### PUT /games/{id}  🔒 *(Admin Only)*
Atualiza um jogo existente.  
Body:
```json
{
  "title": "Forza Horizon 5 - Deluxe",
  "genre": "Racing",
  "price": 349.90,
  "releaseDate": "2023-10-15"
}
```

### DELETE /games/{id}  🔒 *(Admin Only)*
Remove um jogo existente.

---

## 🧪 Testes

Rodar os testes automatizados (xUnit):
```bash
dotnet test
```

Resumo atual:
- ✅ 11 testes passando  
- Cobrem **Create, Get, Update, Delete e validações extras**

---

## 🐳 Docker

### Build da imagem
```bash
docker build -t fcg-games-api .
```

### Rodar container
```bash
docker run -d -p 8080:80 fcg-games-api
```

---

## 📂 Estrutura de pastas

```
fcg-games-api/
├── src/
│   └── FCG.Games.Api/      # Projeto principal (Minimal API)
├── tests/
│   └── FCG.Games.Tests/    # Testes unitários (xUnit)
├── fcg-games-api.sln       # Solution
└── README.md
```

---

## 📜 Licença
Projeto acadêmico desenvolvido para a **FIAP - Pós-Graduação Arquitetura de Software**.  
Uso educacional.
