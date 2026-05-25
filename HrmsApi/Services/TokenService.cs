using HrmsApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HrmsApi.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string token, DateTime expiresAt) GenerateToken(User user)
    {
        var jwtConfig   = _config.GetSection("Jwt");
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMins  = int.Parse(jwtConfig["ExpiryMinutes"] ?? "480");
        var expiresAt   = DateTime.UtcNow.AddMinutes(expiryMins);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             jwtConfig["Issuer"],
            audience:           jwtConfig["Audience"],
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
