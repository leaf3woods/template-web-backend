using Template.Web.Application.Dtos;
using System.Security.Claims;
using Template.Web.Domain.Entities.Account;

namespace Template.Web.Application.Services.Base
{
    public interface IUserService : ICrudAppService<User, Guid, UserReadDto, UserQueryDto, UserRegisterDto, UserUpdateDto>
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