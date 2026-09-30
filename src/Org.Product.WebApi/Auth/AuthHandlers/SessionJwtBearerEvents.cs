using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Domain.Entities.Account;
using Org.Product.Infrastructure.Utilities;

namespace Org.Product.WebApi.Auth.AuthHandlers;

public sealed class SessionJwtBearerEvents : JwtBearerEvents
{
    private readonly ICacheStore<User, Guid> _sessions;

    public SessionJwtBearerEvents(ICacheStore<User, Guid> sessions)
    {
        _sessions = sessions;
    }

    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var userId = context.Principal?.GetUserId();
        var token = context.SecurityToken switch
        {
            JsonWebToken jwt => jwt.EncodedToken,
            JwtSecurityToken jwt => jwt.RawData,
            _ => null,
        };

        if (
            userId is null
            || string.IsNullOrEmpty(token)
            || !await _sessions.MatchesAsync(userId.Value, token)
        )
        {
            context.Fail("The login session is no longer valid.");
        }
    }
}
