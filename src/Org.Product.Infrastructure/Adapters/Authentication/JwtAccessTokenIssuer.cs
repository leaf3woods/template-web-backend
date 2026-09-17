using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Org.Product.Application.Abstractions.Authentication;
using Org.Product.Domain.Shared;
using Org.Product.Infrastructure.Utilities.Options;

namespace Org.Product.Infrastructure.Adapters.Authentication;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly FileSigningKeyProvider _keys;
    private readonly AccessTokenOptions _options;

    public JwtAccessTokenIssuer(FileSigningKeyProvider keys, IOptions<AccessTokenOptions> options)
    {
        _keys = keys;
        _options = options.Value;
    }

    public IssuedAccessToken Issue(Guid userId, IEnumerable<Guid> roleIds)
    {
        Claim[] claims =
        [
            new(CustomClaimsType.UserId, userId.ToString()),
            new(CustomClaimsType.RoleId, string.Join(",", roleIds)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            _options.Issuer, _options.Audience, claims, now, now + _options.Expiration,
            new SigningCredentials(_keys.PrivateKey, SecurityAlgorithms.EcdsaSha256)
        );
        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token), _options.Expiration);
    }
}
