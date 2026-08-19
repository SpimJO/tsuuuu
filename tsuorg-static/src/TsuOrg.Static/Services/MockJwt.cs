using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TsuOrg.Frontend.Services;

internal static class MockJwt
{
    private static readonly SymmetricSecurityKey SigningKey = new(
        Encoding.UTF8.GetBytes("tsuorg-static-mock-signing-key-32b!"));

    public static string AccessToken(MockUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new("name", user.FullName),
            new(ClaimTypes.Email, user.Email),
            new("email", user.Email),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
        };
        if (!string.IsNullOrWhiteSpace(user.College))
            claims.Add(new Claim("college", user.College));

        var token = new JwtSecurityToken(
            issuer: "tsuorg-static",
            audience: "tsuorg-static",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
