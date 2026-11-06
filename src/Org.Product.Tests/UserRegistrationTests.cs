using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Org.Product.Application.Abstractions.Authentication;
using Org.Product.Application.Abstractions.CacheStore;
using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Application.Dtos;
using Org.Product.Application.Services;
using Org.Product.Application.Utilities.MapperProfiles.DtoProfiles;
using Org.Product.Application.Utilities.Options;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Repositories;
using Org.Product.Infrastructure.DomainServices;
using Org.Product.Infrastructure.Repositories;
using Org.Product.Tests.Support;

namespace Org.Product.Tests;

public sealed class UserRegistrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegistrationAndGenericCreate_ReuseTrackedRolesAndCommitOnce(
        bool genericCreate
    )
    {
        var mapper = CreateMapper();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=model_test;Username=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var context = new ApiDbContext(options);
        var memberRole = new Role { Id = Role.MemberRole.Id };
        var adminRole = new Role { Id = Role.AdminRole.Id };
        context.Roles.AttachRange(memberRole, adminRole);
        var roles = new Mock<IRepository<Role>>();
        roles.Setup(repo => repo.Query(true))
            .Returns(new AsyncQuery<Role>(context.Roles.Local));
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var passwords = new PasswordCredentialService();
        var service = new UserService(
            new Repository<User>(context),
            roles.Object,
            unit.Object,
            mapper,
            Mock.Of<ICacheStore<User, Guid>>(),
            Mock.Of<ICacheStore<Captcha, Guid>>(),
            passwords,
            Mock.Of<IAccessTokenIssuer>(),
            Mock.Of<ICaptchaGenerator>(),
            Options.Create(new CaptchaOptions())
        );
        var dto = new UserRegisterDto
        {
            Username = "new-user",
            Password = Convert.ToBase64String([10, 20, 30]),
        };

        if (genericCreate)
        {
            await service.CreateAsync(new UserCreateDto
            {
                Username = dto.Username,
                Password = dto.Password,
                RoleIds = [adminRole.Id, adminRole.Id],
            });
        }
        else
        {
            await service.RegisterAsync(dto);
        }

        var inserted = Assert.Single(context.Users.Local);
        Assert.True(passwords.Verify(inserted, dto.Password));
        var expectedRole = genericCreate ? adminRole : memberRole;
        Assert.Same(expectedRole, Assert.Single(inserted.Roles));
        Assert.Equal(EntityState.Added, context.Entry(inserted).State);
        Assert.All(context.ChangeTracker.Entries<Role>(), entry =>
            Assert.Equal(EntityState.Unchanged, entry.State));
        var association = Assert.Single(context.ChangeTracker.Entries<UserRole>());
        Assert.Equal(EntityState.Added, association.State);
        Assert.Equal(inserted.Id, association.Entity.UserId);
        Assert.Equal(expectedRole.Id, association.Entity.RoleId);
        roles.Verify(repo => repo.Query(true), Times.Once);
        unit.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void MappingRegistrationDto_DoesNotInitializeCredentialsOrRoles()
    {
        var user = CreateMapper()
            .Map<User>(
                new UserRegisterDto
                {
                    Username = "new-user",
                    Password = Convert.ToBase64String([10, 20, 30]),
                }
            );
        Assert.Null(user.Passphrase);
        Assert.Null(user.Salt);
        Assert.Null(user.Roles);
    }

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            config =>
            {
                config.AddProfile<UserDtoProfile>();
                config.CreateMap<User, UserReadDto>();
            },
            NullLoggerFactory.Instance
        ).CreateMapper();
}
