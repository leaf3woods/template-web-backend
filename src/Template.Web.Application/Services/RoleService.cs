using Microsoft.EntityFrameworkCore;
using Template.Web.Application.Auth;
using Template.Web.Application.Dtos;
using Template.Web.Application.Services.Base;
using Template.Web.Core;
using Template.Web.Core.Exceptions;
using Template.Web.Domain.Entities.Account;
using Template.Web.Domain.Utilities;
using Template.Web.Infrastructure.DbContexts;
using Template.Web.Infrastructure.Utilities;

namespace Template.Web.Application.Services
{
    [PermissionDefinition("manage all role resources", ManagedResource.Role)]
    public class RoleService :
        CrudAppService<Role, Guid, RoleReadDto, RoleQueryDto, RoleCreateDto, RoleUpdateDto>,
        IRoleService
    {

        public override async Task<IEnumerable<RoleReadDto>> GetListAsync(RoleQueryDto? queryDto = null)
        {
            var roles = await Queryable
                .Include(r => r.Permissions)
                .ToListAsync();
            return Mapper.Map<IEnumerable<RoleReadDto>>(roles);
        }

        public override async Task<PaginatedList<RoleReadDto>> GetPaginatedListAsync(RoleQueryDto queryDto)
        {
            var roles = await Queryable
                .WhereIf(!string.IsNullOrEmpty(queryDto.Name), x => x.Name.Contains(queryDto.Name!))
                .WhereIf(!string.IsNullOrEmpty(queryDto.Code), x => x.Code.Contains(queryDto.Code!))
                .WhereIf(queryDto.State != null, x => x.State == queryDto.State)
                .Include(r => r.Permissions)
                .ToPaginatedListAsync(queryDto.PageIndex, queryDto.PageSize);
            return Mapper.Map<PaginatedList<RoleReadDto>>(roles);
        }

        [PermissionDefinition("create a role", $"{ManagedResource.Role}.{ManagedAction.Add}.New")]
        public async Task<RoleReadDto?> CreateRoleAsync(RoleCreateDto roleDto)
        {
            //if (!RequireScopeUtil.Scopes.Any(s => !roleDto.ScopeNames.Contains(s.Name)))
            //{
            //    throw new NotAcceptableException("unsupported scope find");
            //}
            var entity = Mapper.Map<Role>(roleDto);
            await DbContext.Roles.AddAsync(entity);
            var index = await DbContext.SaveChangesAsync();
            return index == 0 ? null : Mapper.Map<RoleReadDto>(entity);
        }

        [PermissionDefinition("get role info by id", $"{ManagedResource.Role}.{ManagedAction.Get}.Id")]
        public async Task<RoleReadDto?> GetRoleAsync(Guid id)
        {
            var role = await DbContext.Roles
                .Where(r => r.Id == id)
                .Include(r => r.Permissions)
                .AsNoTracking()
                .FirstOrDefaultAsync();
            return Mapper.Map<RoleReadDto>(role);
        }

        [PermissionDefinition("get all roles", $"{ManagedResource.Role}.{ManagedAction.Get}.All")]
        public async Task<IEnumerable<RoleReadDto>> GetRolesAsync()
        {
            var roles = await DbContext.Roles
                .Include(r => r.Permissions)
                .AsNoTracking()
                .ToArrayAsync();
            return Mapper.Map<IEnumerable<RoleReadDto>>(roles);
        }

        [PermissionDefinition("change role manage scope", $"{ManagedResource.Role}.{ManagedAction.Put}.Scopes")]
        public async Task<int> ModifyRoleScopeAsync(Guid roleId, List<string> permissionNames)
        {
            //if (!RequireScopeUtil.Scopes.Any(s => !permissionNames.Contains(s.Name)))
            //{
            //    throw new NotAcceptableException("unsupported scope find");
            //}
            var role = (await DbContext.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId)) ??
                throw new NotFoundException("role is not exist");
            var targets = await DbContext.Permissions
                .Where(p => permissionNames.Contains(p.Name))
                .ToArrayAsync();
            role.Permissions = targets;
            DbContext.Roles.Update(role);
            var result = await DbContext.SaveChangesAsync();
            return result;
        }

        //[PermissionDefinition("get all supported scopes", $"{ManagedResource.Role}.{ManagedAction.Get}.Scopes")]
        //public IEnumerable<RoleScopeReadDto> GetScopes() =>
        //    Mapper.Map<IEnumerable<RoleScopeReadDto>>(RequireScopeUtil.Scopes);
    }
}