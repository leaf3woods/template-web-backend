using System.Security.Claims;
using Org.Product.Application.Abstractions.Captchas;
using Org.Product.Application.Dtos;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Services.Base;

public interface IUserService
    : ICrudAppService<User, Guid, UserReadDto, UserQueryDto, UserRegisterDto, UserUpdateDto>
{
    Task<UserReadDto?> RegisterAsync(UserRegisterDto registerDto);

    Task<string> LoginAsync(UserLoginDto credential);

    Task LogoutAsync(IEnumerable<Claim> claims, string token);

    Task<UserReadDto?> GetUserAsync(Guid id);

    Task<IEnumerable<UserReadDto>> GetUsersWhereAsync(string? username = null);

    Task<UserReadDto?> ChangeRoleAsync(Guid userId, IEnumerable<Guid> roleIds);

    Task<CaptchaReadDto> GenerateCaptchaAsync(CaptchaType? type = null);

    Task<int> ChangePasswordAsync(ChangePasswordDto passwordDto);

    Task<int> ResetPasswordAsync(Guid userId);
}
