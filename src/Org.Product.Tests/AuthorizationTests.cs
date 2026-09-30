using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Application.Dtos.Cache;
using Org.Product.Application.Services.Base;
using Org.Product.Domain.Entities.Authority;
using Org.Product.Domain.Services;
using Org.Product.Domain.Shared;
using Org.Product.Infrastructure.Adapters.Authentication;
using Org.Product.Infrastructure.DomainServices;
using Org.Product.Tests.Support;
using Org.Product.WebApi.Auth;
using Org.Product.WebApi.Auth.AuthHandlers;
using Org.Product.WebApi.Controllers;
using Role = Org.Product.Domain.Entities.Account.Role;
using User = Org.Product.Domain.Entities.Account.User;

namespace Org.Product.Tests;

public sealed class AuthorizationTests
{
    [Theory]
    [InlineData("menu.delete.Id", HttpStatusCode.OK)]
    [InlineData("menu.delete", HttpStatusCode.OK)]
    [InlineData("menu", HttpStatusCode.OK)]
    [InlineData("delete", HttpStatusCode.Forbidden)]
    [InlineData("menu.del", HttpStatusCode.Forbidden)]
    [InlineData("menu.delete.Id.extra", HttpStatusCode.Forbidden)]
    [InlineData("Menu.delete.Id", HttpStatusCode.Forbidden)]
    [InlineData("", HttpStatusCode.Forbidden)]
    public async Task MenuDelete_RequiresExactOrParentPermission(
        string permission,
        HttpStatusCode expected
    )
    {
        await using var host = await AuthHost.CreateAsync([permission]);
        var token = host.Token();
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));
        Assert.Equal(expected, await host.DeleteMenu(token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplacedAndLoggedOutTokens_AreRejectedIncludingSuper(bool super)
    {
        await using var host = await AuthHost.CreateAsync(["menu"], super);
        var first = host.Token();
        await host.Users.SaveAsync(host.UserId, first, TimeSpan.FromMinutes(5));
        Assert.Equal(HttpStatusCode.OK, await host.DeleteMenu(first));

        var second = host.Token();
        await host.Users.SaveAsync(host.UserId, second, TimeSpan.FromMinutes(5));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.DeleteMenu(first));
        Assert.Equal(HttpStatusCode.OK, await host.DeleteMenu(second));

        Assert.False(await host.Users.RemoveAsync(host.UserId, first));
        Assert.Equal(HttpStatusCode.OK, await host.DeleteMenu(second));
        Assert.True(await host.Users.RemoveAsync(host.UserId, second));
        Assert.Equal(HttpStatusCode.Unauthorized, await host.DeleteMenu(second));
    }

    [Fact]
    public async Task AnonymousWritesAndUnannotatedEndpoints_RequireAuthentication()
    {
        await using var host = await AuthHost.CreateAsync([]);
        Assert.Equal(HttpStatusCode.Unauthorized, await host.DeleteMenu(null));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await host.Client.PostAsync("/api/menu", null)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await host.Client.GetAsync("/fallback")).StatusCode
        );
        Assert.Equal(HttpStatusCode.OK, (await host.Client.GetAsync("/api/menu/tree")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await host.Client.GetAsync("/api/home/captcha")).StatusCode
        );
    }

    [Fact]
    public async Task ActionPermission_DoesNotAlsoRequireResourceWidePermission()
    {
        await using var host = await AuthHost.CreateAsync(["user.get.Id"]);
        var token = host.Token();
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/{host.UserId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("")]
    public async Task InvalidRoleClaims_AreRejected(string roleClaim)
    {
        await using var host = await AuthHost.CreateAsync(["menu"]);
        var token = host.Token(roleClaim);
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));
        Assert.Equal(HttpStatusCode.Forbidden, await host.DeleteMenu(token));
    }

    [Fact]
    public async Task PartiallyInvalidRoleClaims_AreRejected()
    {
        await using var host = await AuthHost.CreateAsync(["menu"]);
        var token = host.Token($"{Role.MemberRole.Id},invalid");
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));

        Assert.Equal(HttpStatusCode.Forbidden, await host.DeleteMenu(token));
    }

    [Fact]
    public async Task MissingSuperRole_DoesNotBypassAuthorization()
    {
        await using var host = await AuthHost.CreateAsync(null, super: true);
        var token = host.Token();
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));
        Assert.Equal(HttpStatusCode.Forbidden, await host.DeleteMenu(token));
    }

    [Fact]
    public async Task MultipleRoles_CombinePermissions()
    {
        var rolePermissions = new Dictionary<Guid, string[]?>
        {
            [Role.MemberRole.Id] = [],
            [Role.AdminRole.Id] = ["menu"],
        };
        await using var host = await AuthHost.CreateAsync([], rolePermissions: rolePermissions);
        var token = host.Token($"{Role.MemberRole.Id},{Role.AdminRole.Id}");
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));

        Assert.Equal(HttpStatusCode.OK, await host.DeleteMenu(token));
    }

    [Fact]
    public async Task MissingOneOfMultipleRoles_RejectsGrantedPermission()
    {
        var rolePermissions = new Dictionary<Guid, string[]?> { [Role.MemberRole.Id] = ["menu"] };
        await using var host = await AuthHost.CreateAsync(
            ["menu"],
            rolePermissions: rolePermissions
        );
        var token = host.Token($"{Role.MemberRole.Id},{Role.AdminRole.Id}");
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));

        Assert.Equal(HttpStatusCode.Forbidden, await host.DeleteMenu(token));
    }

    [Fact]
    public async Task MissingOtherRole_RejectsSuperRole()
    {
        var rolePermissions = new Dictionary<Guid, string[]?> { [Role.SuperRole.Id] = [] };
        await using var host = await AuthHost.CreateAsync([], super: true, rolePermissions);
        var token = host.Token($"{Role.SuperRole.Id},{Role.AdminRole.Id}");
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));

        Assert.Equal(HttpStatusCode.Forbidden, await host.DeleteMenu(token));
    }

    [Fact]
    public async Task LogoutEndpoint_PassesTheAuthenticatedTokenAndRevokesIt()
    {
        await using var host = await AuthHost.CreateAsync(["menu"]);
        var token = host.Token();
        await host.Users.SaveAsync(host.UserId, token, TimeSpan.FromMinutes(5));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/home/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await host.Client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await host.DeleteMenu(token));
    }

    private sealed class AuthHost : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly ECDsa _key;
        private readonly bool _super;
        public Guid UserId { get; } = Guid.NewGuid();
        public ICacheStore<User, Guid> Users { get; }
        public HttpClient Client { get; }

        private AuthHost(WebApplication app, ECDsa key, RedisUserSessionStore users, bool super)
        {
            _app = app;
            _key = key;
            Users = users;
            _super = super;
            Client = app.GetTestClient();
        }

        public static async Task<AuthHost> CreateAsync(
            string[]? permissions,
            bool super = false,
            IReadOnlyDictionary<Guid, string[]?>? rolePermissions = null
        )
        {
            var redis = new RedisStub();
            var users = new RedisUserSessionStore(redis.Connection.Object);
            rolePermissions ??= new Dictionary<Guid, string[]?>
            {
                [super ? Role.SuperRole.Id : Role.MemberRole.Id] = permissions,
            };
            var roles = new Mock<ICacheQuery<Role, Guid, RolePermissionsCacheItem>>();
            roles
                .Setup(service =>
                    service.FindManyWithDetailAsync<RolePermission, Permission>(
                        It.IsAny<IReadOnlyCollection<Guid>>(),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(
                    (IReadOnlyCollection<Guid> roleIds, CancellationToken _) =>
                        Task.FromResult<IReadOnlyDictionary<Guid, RolePermissionsCacheItem>>(
                            roleIds
                                .Where(roleId =>
                                    rolePermissions.TryGetValue(roleId, out var codes)
                                    && codes is not null
                                )
                                .ToDictionary(
                                    roleId => roleId,
                                    roleId => new RolePermissionsCacheItem
                                    {
                                        Id = roleId,
                                        PermissionCodes = rolePermissions[roleId]!,
                                    }
                                )
                        )
                );
            var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();
            builder.Services.AddControllers().AddApplicationPart(typeof(MenuController).Assembly);
            builder.Services.AddSingleton<ICacheStore<User, Guid>>(users);
            builder.Services.AddSingleton(roles.Object);
            var userService = new Mock<IUserService>();
            userService
                .Setup(service =>
                    service.LogoutAsync(It.IsAny<IEnumerable<Claim>>(), It.IsAny<string>())
                )
                .Returns(
                    (IEnumerable<Claim> claims, string token) =>
                        users.RemoveAsync(
                            Guid.Parse(
                                claims.Single(claim => claim.Type == CustomClaimsType.UserId).Value
                            ),
                            token
                        )
                );
            builder.Services.AddSingleton(userService.Object);
            builder.Services.AddSingleton(Mock.Of<IMenuService>());
            builder.Services.AddSingleton(Mock.Of<IRoleService>());
            builder.Services.AddSingleton<
                IAuthorizationDomainService,
                AuthorizationDomainService
            >();
            builder.Services.AddScoped<SessionJwtBearerEvents>();
            builder.Services.AddScoped<IAuthorizationHandler, CustomRequireHandler>();
            builder
                .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.EventsType = typeof(SessionJwtBearerEvents);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new ECDsaSecurityKey(key),
                        ValidIssuer = "tests",
                        ValidAudience = "tests",
                        ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
                    };
                });
            builder.Services.AddAuthorization(options => options.AddAllPolicies());
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapGet("/fallback", () => "ok");
            await app.StartAsync();
            return new AuthHost(app, key, users, super);
        }

        public string Token(string? roleClaim = null)
        {
            var claims = new[]
            {
                new Claim(CustomClaimsType.UserId, UserId.ToString()),
                new Claim(
                    CustomClaimsType.RoleId,
                    roleClaim ?? (_super ? Role.SuperRole.Id : Role.MemberRole.Id).ToString()
                ),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            return new JwtSecurityTokenHandler().WriteToken(
                new JwtSecurityToken(
                    "tests",
                    "tests",
                    claims,
                    DateTime.UtcNow.AddSeconds(-1),
                    DateTime.UtcNow.AddMinutes(5),
                    new SigningCredentials(
                        new ECDsaSecurityKey(_key),
                        SecurityAlgorithms.EcdsaSha256
                    )
                )
            );
        }

        public async Task<HttpStatusCode> DeleteMenu(string? token)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/menu/id/{Guid.NewGuid()}"
            );
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            using var response = await Client.SendAsync(request);
            return response.StatusCode;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
            _key.Dispose();
        }
    }
}
