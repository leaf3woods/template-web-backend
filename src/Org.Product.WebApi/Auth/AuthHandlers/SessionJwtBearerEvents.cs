using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Org.Product.Domain.Services;
using Org.Product.Domain.Shared;

namespace Org.Product.WebApi.Auth.AuthHandlers;

public sealed class SessionJwtBearerEvents : JwtBearerEvents
{
    private readonly IUserDomainService _userDomainService;

    public SessionJwtBearerEvents(IUserDomainService userDomainService)
    {
        _userDomainService = userDomainService;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var userId = context.Principal?.FindFirst(CustomClaimsType.UserId)?.Value;
        var token = context.SecurityToken switch
        {
            JsonWebToken jwt => jwt.EncodedToken,
            JwtSecurityToken jwt => jwt.RawData,
            _ => null,
        };

        if (
            !Guid.TryParse(userId, out var uid)
            || uid == Guid.Empty
            || string.IsNullOrEmpty(token)
            || !await _userDomainService.VerifyTokenAsync(uid, token)
        )
        {
            context.Fail("The login session is no longer valid.");
        }
    }
}
