using System.Security.Claims;
using Org.Product.Application.Dtos;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Services.Base
{
    public interface IUserService
        : ICrudAppService<User, Guid, UserReadDto, UserQueryDto, UserRegisterDto, UserUpdateDto>
    {
        public Task<UserReadDto?> RegisterAsync(UserRegisterDto registerDto);

        public Task<string> LoginAsync(UserLoginDto credential);

        public Task LogoutAsync(IEnumerable<Claim> claims);

        public Task<UserReadDto?> GetUserAsync(Guid id);

        public Task<IEnumerable<UserReadDto>> GetUsersWhereAsync(string? username = null);

        public Task<UserReadDto?> ChangeRoleAsync(Guid userId, IEnumerable<Guid> roleIds);

        public Task<CaptchaReadDto> GenerateCaptchaAsync();

        public Task<int> ChangePasswordAsync(ChangePasswordDto passwordDto);

        public Task<int> ResetPasswordAsync(Guid userId);
    }
}
