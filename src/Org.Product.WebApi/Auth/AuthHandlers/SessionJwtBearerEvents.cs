using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Org.Product.Application.Abstractions.Authentication;
using Org.Product.Domain.Shared;

namespace Org.Product.WebApi.Auth.AuthHandlers;

public sealed class SessionJwtBearerEvents : JwtBearerEvents
{
    private readonly IUserSessionStore _sessions;

    public SessionJwtBearerEvents(IUserSessionStore sessions)
    {
        _sessions = sessions;
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
            || !await _sessions.IsValidAsync(uid, token)
        )
        {
            context.Fail("The login session is no longer valid.");
        }
    }
}
