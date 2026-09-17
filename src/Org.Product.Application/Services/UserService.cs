using System.Security.Claims;
using AutoMapper;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Org.Product.Application.Dtos;
using Org.Product.Application.Services.Base;
using Org.Product.Application.Utilities;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Shared;
using Org.Product.Domain.Shared.Attributes;
using Org.Product.Domain.Shared.Exceptions;
using Org.Product.Domain.Utilities;
using Org.Product.Application.Utilities.Options;
using Org.Product.Application.Abstractions.Authentication;
using Org.Product.Application.Abstractions.Captchas;

namespace Org.Product.Application.Services;

[PermissionDefinition("manage all user resources", ManagedResource.User)]
public class UserService
    : CrudAppService<User, Guid, UserReadDto, UserQueryDto, UserRegisterDto, UserUpdateDto>,
        IUserService
{
    public UserService(
        IRepository<User> repository,
        IRepository<Role> roleRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUserSessionStore sessions,
        ICaptchaChallengeStore challenges,
        IPasswordCredentialService passwords,
        IAccessTokenIssuer tokens,
        ICaptchaGenerator captchaGenerator,
        IOptions<CaptchaOptions> captchaOptions
    )
        : base(repository, unitOfWork, mapper)
    {
        _roleRepository = roleRepository;
        _sessions = sessions;
        _challenges = challenges;
        _passwords = passwords;
        _tokens = tokens;
        _captchaGenerator = captchaGenerator;
        _captchaOptions = captchaOptions.Value;
    }

    private readonly IRepository<Role> _roleRepository;
    private readonly IUserSessionStore _sessions;
    private readonly ICaptchaChallengeStore _challenges;
    private readonly IPasswordCredentialService _passwords;
    private readonly IAccessTokenIssuer _tokens;
    private readonly ICaptchaGenerator _captchaGenerator;
    private readonly CaptchaOptions _captchaOptions;

    public override async Task<IEnumerable<UserReadDto>> GetListAsync(UserQueryDto? query)
    {
        var defaultIds = User.Seeds.Select(u => u.Id).ToList();
        var users = await Queryable
            .WhereIf(
                !string.IsNullOrEmpty(query?.Phone),
                u => u.PhoneNumber == null || u.PhoneNumber.Contains(query!.Phone!)
            )
            .WhereIf(
                !string.IsNullOrEmpty(query?.UserName),
                u => u.Username.Contains(query!.UserName!)
            )
            .WhereIf(!string.IsNullOrEmpty(query?.Name), u => u.Name!.Contains(query!.Name!))
            .WhereIf(query?.State != null, u => u.State == query!.State)
            .Where(u => !defaultIds.Contains(u.Id))
            .Include(u => u.Roles)
            .ToListAsync();
        return Mapper.Map<IEnumerable<UserReadDto>>(users);
    }

    public override async Task<PaginatedList<UserReadDto>> GetPaginatedListAsync(
        UserQueryDto query
    )
    {
        var users = await Queryable
            .Where(u =>
                u.PhoneNumber != null
                && (string.IsNullOrEmpty(query.Phone) || u.PhoneNumber.Contains(query.Phone))
            )
            .Where(u =>
                string.IsNullOrEmpty(query.UserName) || u.Username.Contains(query.UserName)
            )
            .Where(u => string.IsNullOrEmpty(query.Name) || u.Name!.Contains(query.Name))
            .Where(u => query.State == null || u.State == query.State)
            .Where(u => u.Roles.Count() == 1 && u.Roles.Any(r => r.Id == Role.MemberRole.Id))
            .Include(u => u.Roles)
            .ToPaginatedListAsync(query.PageIndex, query.PageSize);
        return Mapper.Map<PaginatedList<UserReadDto>>(users);
    }

    public override Task<UserReadDto?> CreateAsync(UserRegisterDto dto) => RegisterAsync(dto);

    public async Task<UserReadDto?> RegisterAsync(UserRegisterDto registerDto)
    {
        var user = Mapper.Map<User>(registerDto);
        _passwords.SetPassword(user, registerDto.Password);
        user.Roles = [Role.MemberRole];
        await Repository.AddAsync(user);
        var count = await UnitOfWork.SaveChangesAsync();
        return count == 0 ? null : Mapper.Map<UserReadDto>(user);
    }

    public async Task<string> LoginAsync(UserLoginDto credential)
    {
        var answer = Mapper.Map<Captcha>(credential.Captcha);

        if (
            _captchaOptions.RequireVerification
            && (answer is null || !await _challenges.VerifyAsync(answer))
        )
        {
            throw new NotAcceptableException("captcha not found or not correct");
        }
        var user = await Queryable
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == credential.Username);

        if (user is null || !_passwords.Verify(user, credential.Password))
        {
            throw new NotAcceptableException("user not found or password error");
        }
        var token = _tokens.Issue(user.Id, user.Roles.Select(role => role.Id));
        await _sessions.SaveAsync(user.Id, token.Value, token.Lifetime);
        return token.Value;
    }

    public async Task LogoutAsync(IEnumerable<Claim> claims, string token)
    {
        var userId = claims.FirstOrDefault(c => c.Type == CustomClaimsType.UserId)?.Value;
        if (!Guid.TryParse(userId, out var uid) || uid == Guid.Empty || string.IsNullOrEmpty(token))
        {
            throw new ForbiddenException("invalid login session");
        }
        await _sessions.RevokeAsync(uid, token);
    }

    public override async Task<int> DeleteAsync(Guid key)
    {
        var count = await base.DeleteAsync(key);
        await _sessions.RevokeAsync(key);
        return count;
    }

    public override async Task<UserReadDto?> UpdateAsync(Guid key, UserUpdateDto dto)
    {
        var entity = await Repository.FindAsync(key);
        if (entity is null)
        {
            return null;
        }

        Mapper.Map(dto, entity);
        Repository.Update(entity);
        await UnitOfWork.SaveChangesAsync();
        await _sessions.RevokeAsync(key);
        return Mapper.Map<UserReadDto>(entity);
    }

    [PermissionDefinition(
        "get single user by id",
        $"{ManagedResource.User}.{ManagedAction.Get}.Id"
    )]
    public async Task<UserReadDto?> GetUserAsync(Guid id)
    {
        var user = await Queryable
            .Where(u => u.Id == id)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync();
        return Mapper.Map<UserReadDto>(user);
    }

    [PermissionDefinition(
        "get users where",
        $"{ManagedResource.User}.{ManagedAction.Get}.Query"
    )]
    public async Task<IEnumerable<UserReadDto>> GetUsersWhereAsync(string? name = null)
    {
        var users = await Queryable
            .Where(u =>
                string.IsNullOrEmpty(name)
                || u.Username.Contains(name)
                || u.Nick == null
                || u.Nick.Contains(name)
            )
            .Include(u => u.Roles)
            .ToArrayAsync();
        return Mapper.Map<IEnumerable<UserReadDto>>(users);
    }

    [PermissionDefinition(
        "change user role",
        $"{ManagedResource.User}.{ManagedAction.Put}.Role"
    )]
    public async Task<UserReadDto?> ChangeRoleAsync(Guid userId, IEnumerable<Guid> roleIds)
    {
        var requestedRoleIds = roleIds.Distinct().ToArray();
        var user =
            (await Queryable.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId))
            ?? throw new NotFoundException("user not found");
        var roles = await _roleRepository
            .Query()
            .Where(r => requestedRoleIds.Contains(r.Id))
            .ToArrayAsync();
        if (roles.Length == 0 || roles.Length != requestedRoleIds.Length)
        {
            throw new NotFoundException("role not found");
        }

        user.Roles = roles;
        Repository.Update(user);
        var count = await UnitOfWork.SaveChangesAsync();
        await _sessions.RevokeAsync(userId);
        return count == 0 ? null : Mapper.Map<UserReadDto>(user);
    }

    public async Task<CaptchaReadDto> GenerateCaptchaAsync()
    {
        var captcha = _captchaGenerator.Generate();
        await _challenges.SaveAsync(captcha, _captchaOptions.Expiration);
        return Mapper.Map<CaptchaReadDto>(captcha);
    }

    public async Task<int> ChangePasswordAsync(ChangePasswordDto passwordDto)
    {
        var answer = Mapper.Map<Captcha>(passwordDto.Captcha);

        if (
            _captchaOptions.RequireVerification
            && (answer is null || !await _challenges.VerifyAsync(answer))
        )
        {
            throw new NotAcceptableException("captcha not exist or not correct");
        }
        var user =
            await Queryable.FirstOrDefaultAsync(u => u.Username == passwordDto.Username)
            ?? throw new NotFoundException("user not found");
        if (!_passwords.Verify(user, passwordDto.OldPassword))
        {
            throw new NotAcceptableException("password error");
        }

        _passwords.SetPassword(user, passwordDto.NewPassword);
        Repository.Update(user);
        var count = await UnitOfWork.SaveChangesAsync();
        await _sessions.RevokeAsync(user.Id);
        return count;
    }

    [PermissionDefinition(
        "reset someone's password",
        $"{ManagedResource.User}.{ManagedAction.Put}.ResetPwd"
    )]
    public async Task<int> ResetPasswordAsync(Guid userId)
    {
        var user =
            await Repository.FindAsync(userId) ?? throw new NotFoundException("user not found");
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("12345678")));
        _passwords.SetPassword(user, hash);
        Repository.Update(user);
        var count = await UnitOfWork.SaveChangesAsync();
        await _sessions.RevokeAsync(user.Id);
        return count;
    }
}
