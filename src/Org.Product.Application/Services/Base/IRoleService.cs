using Org.Product.Application.Dtos;
using Org.Product.Domain.Entities.Account;

namespace Org.Product.Application.Services.Base;

public interface IRoleService
    : ICrudAppService<Role, Guid, RoleReadDto, RoleQueryDto, RoleCreateDto, RoleUpdateDto>
{
    Task<RoleReadDto?> GetRoleAsync(Guid id);

    Task<IEnumerable<RoleReadDto>> GetRolesAsync();

    Task<RoleReadDto?> CreateRoleAsync(RoleCreateDto roleDto);

    Task<int> ModifyRoleScopeAsync(Guid roleId, List<string> scopeName);

    //public IEnumerable<RoleScopeReadDto> GetScopes();
}
