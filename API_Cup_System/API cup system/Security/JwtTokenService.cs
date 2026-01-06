using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using HotChocolate;
using HotChocolate.Execution;
using TournamentApi.Domain;

namespace TournamentApi.Security;

public class JwtTokenService
{
    private readonly JwtOptions _opt;

    public JwtTokenService(IOptions<JwtOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public string CreateToken(User user)
    {
        var claims = BuildClaims(user);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.ExpiresMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

  
    public static int GetUserId(ClaimsPrincipal principal)
    {
        var subClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                       ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subClaim) || !int.TryParse(subClaim, out var id))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Unauthorized")
                    .SetCode("UNAUTHORIZED")
                    .Build()
            );
        }

        return id;
    }

    
    private static List<Claim> BuildClaims(User user)
    {
        return new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };
    }
}
