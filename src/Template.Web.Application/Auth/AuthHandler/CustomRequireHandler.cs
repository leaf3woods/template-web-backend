using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Template.Web.Application.Auth.Requirements;
using Template.Web.Application.Services.Base;
using Template.Web.Core;
using Template.Web.Domain.Entities.Account;
using Template.Web.Infrastructure.DbContexts;


namespace BcsJiaer.Application.Auth.AuthHandler
{
    public class CustomRequireHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
    {
        public CustomRequireHandler(
            ApiDbContext apiDbContext,
            ILogger<CustomRequireHandler> logger
            )
        {
            _apiDbContext = apiDbContext;
            _logger = logger;
        }

        private readonly ApiDbContext _apiDbContext;
        private readonly ILogger _logger;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermissionAuthorizationRequirement requirement)
        {
            var dict = context.User.Claims.ToDictionary(key => key.Type, value => value.Value);
            if (!dict.TryGetValue(CustomClaimsType.UserId, out var uId) ||
                !dict.TryGetValue(CustomClaimsType.RoleId, out var rIds))
            {
                _logger.LogWarning("invalid token claims");
                context.Fail(new AuthorizationFailureReason(this, "invalid claims"));
                return;
            }
            var userId = Guid.Parse(uId);
            var roleIds = rIds.Split(",").Select(Guid.Parse);

            if (roleIds.Contains(Role.SuperRole.Id))
            {
                context.Succeed(requirement);
                return;
            }

            var roles = await _apiDbContext.Roles
                .Where(r => roleIds.Contains(r.Id))
                .ToArrayAsync();
            if (roles?.Length == 0)
            {
                _logger.LogWarning("a token with invalid role");
                context.Fail(new AuthorizationFailureReason(this, "unknow role"));
                return;
            }
            var permissions = roles!.SelectMany(r => r.Permissions!).DistinctBy(p => p.Code).ToArray();
            if (permissions.Any(s => requirement.Permission.Contains(s.Name)))
            {
                _logger.LogTrace("scope match success");
                context.Succeed(requirement);
                return;
            }
            context.Fail(new AuthorizationFailureReason(this, "no authorization"));
        }
    }
}