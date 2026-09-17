using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Org.Product.Application.Dtos;
using Org.Product.Application.Services.Base;
using Org.Product.Application.Utilities;
using Org.Product.Domain.Entities.Account;
using Org.Product.Domain.Entities.Authority;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Shared;
using Org.Product.Domain.Shared.Attributes;
using Org.Product.Domain.Shared.Exceptions;
using Org.Product.Domain.Utilities;

namespace Org.Product.Application.Services;

[PermissionDefinition("manage all role resources", ManagedResource.Role)]
public class RoleService
    : CrudAppService<Role, Guid, RoleReadDto, RoleQueryDto, RoleCreateDto, RoleUpdateDto>,
        IRoleService
{
    private readonly IRepository<Permission> _permissionRepository;

    public RoleService(
        IRepository<Role> repository,
        IRepository<Permission> permissionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper
    )
        : base(repository, unitOfWork, mapper)
    {
        _permissionRepository = permissionRepository;
    }

    public override async Task<IEnumerable<RoleReadDto>> GetListAsync(
        RoleQueryDto? queryDto = null
    )
    {
        var roles = await Queryable.Include(r => r.Permissions).ToListAsync();
        return Mapper.Map<IEnumerable<RoleReadDto>>(roles);
    }

    public override async Task<PaginatedList<RoleReadDto>> GetPaginatedListAsync(
        RoleQueryDto queryDto
    )
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
        await Repository.AddAsync(entity);
        var index = await UnitOfWork.SaveChangesAsync();
        return index == 0 ? null : Mapper.Map<RoleReadDto>(entity);
    }

    [PermissionDefinition(
        "get role info by id",
        $"{ManagedResource.Role}.{ManagedAction.Get}.Id"
    )]
    public async Task<RoleReadDto?> GetRoleAsync(Guid id)
    {
        var role = await Repository
            .Query(tracking: false)
            .Where(r => r.Id == id)
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync();
        return Mapper.Map<RoleReadDto>(role);
    }

    [PermissionDefinition("get all roles", $"{ManagedResource.Role}.{ManagedAction.Get}.All")]
    public async Task<IEnumerable<RoleReadDto>> GetRolesAsync()
    {
        var roles = await Repository
            .Query(tracking: false)
            .Include(r => r.Permissions)
            .ToArrayAsync();
        return Mapper.Map<IEnumerable<RoleReadDto>>(roles);
    }

    [PermissionDefinition(
        "change role manage scope",
        $"{ManagedResource.Role}.{ManagedAction.Put}.Scopes"
    )]
    public async Task<int> ModifyRoleScopeAsync(Guid roleId, List<string> permissionNames)
    {
        //if (!RequireScopeUtil.Scopes.Any(s => !permissionNames.Contains(s.Name)))
        //{
        //    throw new NotAcceptableException("unsupported scope find");
        //}
        var role =
            (
                await Repository
                    .Query()
                    .Include(r => r.Permissions)
                    .FirstOrDefaultAsync(r => r.Id == roleId)
            ) ?? throw new NotFoundException("role is not exist");
        var targets = await _permissionRepository
            .Query()
            .Where(p => permissionNames.Contains(p.Name))
            .ToArrayAsync();
        role.Permissions = targets;
        Repository.Update(role);
        var result = await UnitOfWork.SaveChangesAsync();
        return result;
    }

    //[PermissionDefinition("get all supported scopes", $"{ManagedResource.Role}.{ManagedAction.Get}.Scopes")]
    //public IEnumerable<RoleScopeReadDto> GetScopes() =>
    //    Mapper.Map<IEnumerable<RoleScopeReadDto>>(RequireScopeUtil.Scopes);
}
