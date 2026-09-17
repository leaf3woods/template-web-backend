using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Org.Product.Application.Abstractions.Authentication;
using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Application.Abstractions.Security;
using Org.Product.Application.Dtos;
using Org.Product.Application.Services;
using Org.Product.Application.Utilities.MapperProfiles.DtoProfiles;
using Org.Product.Application.Utilities.Options;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Repositories;
using Org.Product.Infrastructure.Adapters.Security;

namespace Org.Product.Tests;

public sealed class UserRegistrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegistrationAndGenericCreate_InitializeCredentialsAndCommitOnce(bool genericCreate)
    {
        var mapper = CreateMapper();
        User? inserted = null;
        var repository = new Mock<IRepository<User>>();
        repository.Setup(repo => repo.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => inserted = user)
            .Returns(Task.CompletedTask);
        var unit = new Mock<IUnitOfWork>();
        unit.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var passwords = new PasswordCredentialService();
        var service = new UserService(repository.Object, Mock.Of<IRepository<Role>>(),
            unit.Object, mapper, Mock.Of<IUserSessionStore>(), Mock.Of<ICaptchaChallengeStore>(),
            passwords, Mock.Of<IAccessTokenIssuer>(), Mock.Of<ICaptchaGenerator>(),
            Options.Create(new CaptchaOptions()));
        var dto = new UserRegisterDto
        {
            Username = "new-user",
            Password = Convert.ToBase64String([10, 20, 30]),
        };

        if (genericCreate)
        {
            await service.CreateAsync(dto);
        }
        else
        {
            await service.RegisterAsync(dto);
        }

        Assert.NotNull(inserted);
        Assert.True(passwords.Verify(inserted, dto.Password));
        Assert.Equal(Role.MemberRole.Id, Assert.Single(inserted.Roles).Id);
        unit.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void MappingRegistrationDto_DoesNotInitializeCredentialsOrRoles()
    {
        var user = CreateMapper().Map<User>(new UserRegisterDto
        {
            Username = "new-user",
            Password = Convert.ToBase64String([10, 20, 30]),
        });
        Assert.Null(user.Passphrase);
        Assert.Null(user.Salt);
        Assert.Null(user.Roles);
    }

    private static IMapper CreateMapper() => new MapperConfiguration(config =>
    {
        config.AddProfile<UserDtoProfile>();
        config.CreateMap<User, UserReadDto>();
    }, NullLoggerFactory.Instance).CreateMapper();
}
