using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FCG.Games.Tests
{
    public static class TestJwtHelper
    {
        private const string Key = "mysupersecuretestkey12345678901234567890"; // >= 32 chars
        private const string Issuer = "fcg-games-api"; // igual ao Program.cs

        public static string GenerateAdminToken()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "TestAdmin"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            return GenerateToken(claims);
        }

        public static string GenerateUserToken()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "User")
            };

            return GenerateToken(claims);
        }

        private static string GenerateToken(Claim[] claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: null,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
