using Template.Web.Application.Dtos;
using Template.Web.Domain.Entities.Account;

namespace Template.Web.Application.Services.Base
{
    public interface IRoleService :
        ICrudAppService<Role, Guid, RoleReadDto, RoleQueryDto, RoleCreateDto, RoleUpdateDto>
    {
        public Task<RoleReadDto?> GetRoleAsync(Guid id);

        public Task<IEnumerable<RoleReadDto>> GetRolesAsync();

        public Task<RoleReadDto?> CreateRoleAsync(RoleCreateDto roleDto);

        public Task<int> ModifyRoleScopeAsync(Guid roleId, List<string> scopeName);

        //public IEnumerable<RoleScopeReadDto> GetScopes();
    }
}