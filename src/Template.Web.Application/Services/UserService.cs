using Template.Web.Application.Auth;
using Template.Web.Application.Captchas;
using Template.Web.Application.Captchas.Builder;
using Template.Web.Application.Dtos;
using Template.Web.Application.Services.Base;
using Template.Web.Application.Utilities;
using Template.Web.Core;
using Template.Web.Core.Exceptions;
using Template.Web.Core.Utilities;
using Template.Web.Domain.Services;
using Template.Web.Domain.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Template.Web.Infrastructure.DbContexts;
using Template.Web.Domain.Entities.Account;
using Template.Web.Infrastructure.Utilities;

namespace Template.Web.Application.Services
{
    [PermissionDefinition("manage all user resources", ManagedResource.User)]
    public class UserService : CrudAppService<User, Guid, UserReadDto, UserQueryDto, UserRegisterDto, UserUpdateDto>, IUserService
    {
        public UserService(
            IUserDomainService userDomainService
            )
        {
            _userDomainService = userDomainService;
        }
        
        private readonly IUserDomainService _userDomainService;

        public override async Task<IEnumerable<UserReadDto>> GetListAsync(UserQueryDto? query)
        {
            var defaultIds = User.Seeds.Select(u => u.Id).ToList();
            var users = await Queryable
                .WhereIf(!string.IsNullOrEmpty(query?.Phone),
                    u => u.PhoneNumber == null || u.PhoneNumber.Contains(query!.Phone!))
                .WhereIf(!string.IsNullOrEmpty(query?.UserName), u => u.Username.Contains(query!.UserName!))
                .WhereIf(!string.IsNullOrEmpty(query?.Name), u => u.Name!.Contains(query!.Name!))
                .WhereIf(query?.State != null, u => u.State == query!.State)
                .Where(u => !defaultIds.Contains(u.Id))
                .Include(u => u.Roles)
                .ToListAsync();
            return Mapper.Map<IEnumerable<UserReadDto>>(users);
        }

        public override async Task<PaginatedList<UserReadDto>> GetPaginatedListAsync(UserQueryDto query)
        {
            var users = await Queryable
                .Where(u => u.PhoneNumber != null &&
                            (string.IsNullOrEmpty(query.Phone) || u.PhoneNumber.Contains(query.Phone)))
                .Where(u => string.IsNullOrEmpty(query.UserName) || u.Username.Contains(query.UserName))
                .Where(u => string.IsNullOrEmpty(query.Name) || u.Name!.Contains(query.Name))
                .Where(u => query.State == null || u.State == query.State)
                .Where(u => u.Roles.Count() == 1 && u.Roles.Any(r => r.Id == Role.MemberRole.Id))
                .Include(u => u.Roles)
                .ToPaginatedListAsync(query.PageIndex, query.PageSize);
            return Mapper.Map<PaginatedList<UserReadDto>>(users);
        }

        public async Task<UserReadDto?> RegisterAsync(UserRegisterDto registerDto)
        {
            var user = Mapper.Map<User>(registerDto);
            await DbContext.Users.AddAsync(user);
            var count = await DbContext.SaveChangesAsync();
            return count == 0 ? null : Mapper.Map<UserReadDto>(user);
        }

        public async Task<string> LoginAsync(UserLoginDto credential)
        {
            var answer = Mapper.Map<Captcha>(credential.Captcha);

            if (!InitialConfiguration.IsDevelopment
                && (answer is null || !await _userDomainService.VerifyCaptchaAnswerAsync(answer)))
            {
                throw new NotAcceptableException("captcha not found or not correct");
            }
            var user = await DbContext.Users.Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username == credential.Username);
            var bytes = Convert.FromBase64String(credential.Password);

            if (user is null || !user.Verify(bytes))
            {
                throw new NotAcceptableException("user not found or password error");
            }
            var rids = string.Join(",", user.Roles.Select(r => r.Id));
            var token = JwtTokenUtil.GenerateJwtToken(
                InitialConfiguration.Jwt.Issuer, InitialConfiguration.Jwt.Audience, InitialConfiguration.Jwt.ExpireMin,
                new Claim(CustomClaimsType.UserId, user.Id.ToString()), new Claim(CustomClaimsType.RoleId, rids)) ??
                throw new Exception("generate jwt token error");

            if (!await _userDomainService.VerifyTokenAsync(user.Id, token))
            {
                throw new ForbiddenException("user was logged in elsewhere");
            }
            await _userDomainService.CacheTokenAsync(user.Id, token);
            return token;
        }

        public async Task LogoutAsync(IEnumerable<Claim> claims)
        {
            var userId = claims.FirstOrDefault(c => c.Type == CustomClaimsType.UserId)!.Value;
            await _userDomainService.DeleteTokenAsync(Guid.Parse(userId)!);
        }

        [PermissionDefinition("get single user by id", $"{ManagedResource.User}.{ManagedAction.Get}.Id")]
        public async Task<UserReadDto?> GetUserAsync(Guid id)
        {
            var user = await DbContext.Users
                .Where(u => u.Id == id)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync();
            return Mapper.Map<UserReadDto>(user);
        }

        [PermissionDefinition("get users where", $"{ManagedResource.User}.{ManagedAction.Get}.Query")]
        public async Task<IEnumerable<UserReadDto>> GetUsersWhereAsync(string? name = null)
        {
            var users = await DbContext.Users
                .Where(u => string.IsNullOrEmpty(name) || u.Username.Contains(name) ||
                u.Nick == null || u.Nick.Contains(name))
                .Include(u => u.Roles)
                .ToArrayAsync();
            return Mapper.Map<IEnumerable<UserReadDto>>(users);
        }

        [PermissionDefinition("change user role", $"{ManagedResource.User}.{ManagedAction.Put}.Role")]
        public async Task<UserReadDto?> ChangeRoleAsync(Guid userId, IEnumerable<Guid> roleIds)
        {
            var user = (await DbContext.Users.FindAsync(userId)) ??
                throw new NotFoundException("user not found");
            var roles = await DbContext.Roles
                .Where(r => roleIds.Contains(r.Id))
                .ToArrayAsync();
            if (roles?.Length == 0)
                throw new NotFoundException("role not found");
            user.Roles = roles!;
            DbContext.Users.Update(user);
            var count = await DbContext.SaveChangesAsync();
            return count == 0 ? null : Mapper.Map<UserReadDto>(user);
        }

        public async Task<CaptchaReadDto> GenerateCaptchaAsync()
        {
            //var builder = CaptchaBuilder.Create<CharacterCaptchaBuilder>()
            //    .WithLowerCase()
            //    .WithUpperCase();
            var builder = CaptchaBuilder.Create<QuestionCaptchaBuilder>();
            var captcha = builder.WithGenOption(new CaptchaGenOptions
            {
                FontFamily = "consolas",
                Height = 80,
                Width = 200,
            }).WithNoise().WithLines().WithCircles().Build();
            await _userDomainService.CacheCaptchaAnswerAsync(captcha, 180);
            return Mapper.Map<CaptchaReadDto>(captcha);
        }

        public async Task<int> ChangePasswordAsync(ChangePasswordDto passwordDto)
        {
            var answer = Mapper.Map<Captcha>(passwordDto.Captcha);

            if (!InitialConfiguration.IsDevelopment
                && (answer is null || !await _userDomainService.VerifyCaptchaAnswerAsync(answer)))
            {
                throw new NotAcceptableException("captcha not exist or not correct");
            }
            var user = await DbContext.Users.FindAsync(passwordDto.Username) ??
                throw new NotFoundException("user not found");
            var bytes = Convert.FromBase64String(passwordDto.OldPassword);
            if (!user.Verify(bytes))
            {
                throw new NotAcceptableException("password error");
            }

            _userDomainService.WithSalt(ref user, passwordDto.NewPassword);
            DbContext.Users.Update(user);
            return await DbContext.SaveChangesAsync();
        }

        [PermissionDefinition("reset someone's password", $"{ManagedResource.User}.{ManagedAction.Put}.ResetPwd")]
        public async Task<int> ResetPasswordAsync(Guid userId)
        {
            var user = await DbContext.Users.FindAsync(userId) ??
                throw new NotFoundException("user not found");
            var hash = CryptoUtil.Sha256("12345678");
            _userDomainService.WithSalt(ref user, hash);
            DbContext.Users.Update(user);
            return await DbContext.SaveChangesAsync();
        }
    }
}