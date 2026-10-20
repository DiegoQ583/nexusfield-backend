using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NexusField.Application.DTOs.Auth;

namespace NexusField.API.Providers;

public class JwtProvider(IConfiguration configuration)
{
    private static readonly TimeSpan DuracionToken = TimeSpan.FromHours(8);

    public string GenerarToken(LoginResponseDto usuario)
    {
        var secret = configuration["JWT_SECRET"]
            ?? throw new InvalidOperationException("La variable JWT_SECRET no está configurada en el archivo .env.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["JWT_ISSUER"],
            audience: configuration["JWT_AUDIENCE"],
            claims: claims,
            expires: DateTime.UtcNow.Add(DuracionToken),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
