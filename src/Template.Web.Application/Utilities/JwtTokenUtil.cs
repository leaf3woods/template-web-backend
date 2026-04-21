using Template.Web.Domain.Utilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Template.Web.Application.Utilities
{
    public static class JwtTokenUtil
    {
        public static string GenerateJwtToken(JwtPayload pairs)
        {
            var credentials = new SigningCredentials(new ECDsaSecurityKey(CryptoUtil.PrivateECDsa), SecurityAlgorithms.EcdsaSha256);
            var token = new JwtSecurityToken(new JwtHeader(credentials), pairs);
            return token.ToString();
        }

        public static string GenerateJwtToken(string issuer, string audience, TimeSpan expires, params Claim[] claims)
        {
            var credentials = new SigningCredentials(new ECDsaSecurityKey(CryptoUtil.PrivateECDsa), SecurityAlgorithms.EcdsaSha256);
            var nbf = DateTime.UtcNow;
            var expire = nbf + expires;
            var token = new JwtSecurityToken(issuer, audience,
                claims, nbf, expire, credentials);
            var encode = new JwtSecurityTokenHandler().WriteToken(token);
            return encode;
        }
    }
}