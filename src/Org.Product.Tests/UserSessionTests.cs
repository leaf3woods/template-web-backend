using AutoMapper;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Moq;
using Org.Product.Application.Dtos;
using Org.Product.Application.Options;
using Org.Product.Application.Services;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Shared;
using Org.Product.Domain.Shared.Exceptions;
using Org.Product.Domain.Utilities;
using Org.Product.Infrastructure.DomainServices;
using Org.Product.Tests.Support;

namespace Org.Product.Tests;

public sealed class UserSessionTests
{
    [Fact]
    public async Task LoginAgain_CreatesDistinctTokenAndReplacesSession()
    {
        var fixture = new Fixture();
        var credentials = new UserLoginDto { Username = fixture.User.Username, Password = fixture.Password };
        var first = await fixture.Service.LoginAsync(credentials);
        var second = await fixture.Service.LoginAsync(credentials);

        Assert.NotEqual(first, second);
        Assert.False(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, first));
        Assert.True(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, second));
    }

    [Fact]
    public async Task InvalidPassword_DoesNotReplaceExistingSession()
    {
        var fixture = new Fixture();
        await fixture.CacheSessionAsync();
        await Assert.ThrowsAsync<NotAcceptableException>(() => fixture.Service.LoginAsync(new UserLoginDto
        {
            Username = fixture.User.Username,
            Password = Convert.ToBase64String([1, 2, 3]),
        }));
        Assert.True(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, "session"));
    }

    [Fact]
    public async Task Logout_WithOlderToken_DoesNotRemoveNewSession()
    {
        var fixture = new Fixture();
        await fixture.CacheSessionAsync();
        var claims = new[] { new Claim(CustomClaimsType.UserId, fixture.User.Id.ToString()) };

        await fixture.Service.LogoutAsync(claims, "older-session");
        Assert.True(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, "session"));

        await fixture.Service.LogoutAsync(claims, "session");
        Assert.False(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, "session"));
    }

    [Fact]
    public async Task ChangeRole_RevokesOldClaimsAfterSave()
    {
        var fixture = new Fixture();
        await fixture.CacheSessionAsync();
        var role = new Role { Id = Guid.NewGuid() };
        fixture.Roles.Add(role);
        await fixture.Service.ChangeRoleAsync(fixture.User.Id, [role.Id]);
        Assert.Equal(role.Id, fixture.User.Roles.Single().Id);
        Assert.False(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, "session"));
    }

    [Fact]
    public async Task ChangeRole_WithMissingRole_DoesNotChangeRolesOrRevokeSession()
    {
        var fixture = new Fixture();
        await fixture.CacheSessionAsync();
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.ChangeRoleAsync(
            fixture.User.Id, [fixture.Roles[0].Id, Guid.NewGuid()]));
        Assert.True(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, "session"));
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_RevokesSessionAfterSave()
    {
        var fixture = new Fixture();
        await fixture.CacheSessionAsync();
        var newPassword = Convert.ToBase64String([4, 5, 6]);
        await fixture.Service.ChangePasswordAsync(new ChangePasswordDto
        {
            Username = fixture.User.Username, OldPassword = fixture.Password, NewPassword = newPassword,
        });
        Assert.True(fixture.User.Verify(Convert.FromBase64String(newPassword)));
        Assert.False(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, "session"));
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("update")]
    [InlineData("reset-password")]
    public async Task UserMutation_RevokesExistingSession(string operation)
    {
        var fixture = new Fixture();
        await fixture.CacheSessionAsync();
        switch (operation)
        {
            case "delete": await fixture.Service.DeleteAsync(fixture.User.Id); break;
            case "update": await fixture.Service.UpdateAsync(fixture.User.Id, new UserUpdateDto()); break;
            case "reset-password": await fixture.Service.ResetPasswordAsync(fixture.User.Id); break;
        }
        Assert.False(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, "session"));
    }

    [Fact]
    public async Task FailedSave_DoesNotRevokeSession()
    {
        var fixture = new Fixture();
        await fixture.CacheSessionAsync();
        fixture.UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("save failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteAsync(fixture.User.Id));
        Assert.True(await fixture.Sessions.VerifyTokenAsync(fixture.User.Id, "session"));
    }

    private sealed class Fixture
    {
        public User User { get; } = new() { Id = Guid.NewGuid(), Username = "test" };
        public List<Role> Roles { get; } = [new Role { Id = Guid.NewGuid() }];
        public string Password { get; } = Convert.ToBase64String([10, 20, 30]);
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
        public UserDomainService Sessions { get; }
        public UserService Service { get; }

        public Fixture()
        {
            User.Roles = Roles.ToArray();
            User.Passphrase = Convert.ToBase64String(CryptoUtil.Salt(Convert.FromBase64String(Password), out var salt));
            User.Salt = Convert.ToBase64String(salt);
            Sessions = new UserDomainService(new RedisStub().Connection.Object);
            var users = new Mock<IRepository<User>>();
            users.Setup(repository => repository.Query(It.IsAny<bool>())).Returns(new AsyncQuery<User>([User]));
            users.Setup(repository => repository.FindAsync(It.IsAny<object?[]>())).ReturnsAsync(User);
            var roles = new Mock<IRepository<Role>>();
            roles.Setup(repository => repository.Query(It.IsAny<bool>())).Returns(new AsyncQuery<Role>(Roles));
            UnitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            Service = new UserService(users.Object, roles.Object, UnitOfWork.Object, Mock.Of<IMapper>(), Sessions,
                Options.Create(new AccessTokenOptions { Issuer = "tests", Audience = "tests", ExpireMin = 5 }),
                Options.Create(new CaptchaOptions { RequireVerification = false }));
        }

        public Task CacheSessionAsync() => Sessions.CacheTokenAsync(User.Id, "session", TimeSpan.FromMinutes(5));
    }
}
