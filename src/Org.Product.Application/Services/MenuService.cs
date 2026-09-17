using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Org.Product.Application.Dtos;
using Org.Product.Application.Services.Base;
using Org.Product.Application.Utilities;
using Org.Product.Domain.Entities.Authority;
using Org.Product.Domain.Repositories;
using Org.Product.Domain.Shared;
using Org.Product.Domain.Shared.Exceptions;

namespace Org.Product.Application.Services;

public class MenuService
    : CrudAppService<Permission, Guid, MenuReadDto, MenuQueryDto, MenuCreateDto, MenuUpdateDto>,
        IMenuService
{
    private readonly IRepository<Permission> _permissionRepo;

    public MenuService(
        IRepository<Permission> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper
    )
        : base(repository, unitOfWork, mapper)
    {
        _permissionRepo = repository;
    }

    public override async Task<IEnumerable<MenuReadDto>> GetListAsync(
        MenuQueryDto? queryDto = null
    )
    {
        var permissions = await Queryable.ToListAsync();
        return Mapper.Map<IEnumerable<MenuReadDto>>(permissions);
    }

    public override async Task<PaginatedList<MenuReadDto>> GetPaginatedListAsync(
        MenuQueryDto queryDto
    )
    {
        var permissions = await Queryable
            .WhereIf(!string.IsNullOrEmpty(queryDto.Name), x => x.Name.Contains(queryDto.Name!))
            .WhereIf(!string.IsNullOrEmpty(queryDto.Code), x => x.Code.Contains(queryDto.Code!))
            .WhereIf(queryDto.ParentId != null, x => x.ParentId == queryDto.ParentId)
            .WhereIf(queryDto.Type != null, x => x.Type == (PermissionType)queryDto.Type!.Value)
            .WhereIf(queryDto.Visible != null, x => x.Visible == queryDto.Visible)
            .WhereIf(queryDto.State != null, x => x.State == queryDto.State)
            .ToPaginatedListAsync(queryDto.PageIndex, queryDto.PageSize);
        return Mapper.Map<PaginatedList<MenuReadDto>>(permissions);
    }

    public async Task<IEnumerable<MenuReadDto>> GetAllMenusAsync()
    {
        var entities = await Queryable.ToArrayAsync();
        return Mapper.Map<IEnumerable<MenuReadDto>>(entities);
    }

    public async Task<IEnumerable<MenuReadDto>> GetRootMenusAsTreeAsync()
    {
        var entities = await Queryable.ToArrayAsync();
        var menus = Mapper.Map<IEnumerable<MenuReadDto>>(entities);
        var root = AsTree(menus.Single(m => m.ParentId == null));
        return root.Children!;

        MenuReadDto AsTree(MenuReadDto parent)
        {
            parent.Children = menus
                .Where(m => m.ParentId == parent.Id)
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Level)
                .Select(m => AsTree(m));
            return parent;
        }
    }

    public Task<int> SetMenuRouteAsync()
    {
        throw new NotImplementedException();
    }

    public override async Task<int> DeleteAsync(Guid key)
    {
        var menu =
            await _permissionRepo.FindAsync(key) ?? throw new NotFoundException("id not exist");

        if (menu.ParentId is null)
            throw new NotAcceptableException("root menu can't delete");

        if (await _permissionRepo.Query().AnyAsync(m => m.ParentId == key))
        {
            throw new NotAcceptableException("child menu exist");
        }

        _permissionRepo.Remove(menu);
        return await UnitOfWork.SaveChangesAsync();
    }

    public override async Task<MenuReadDto?> UpdateAsync(Guid key, MenuUpdateDto dto)
    {
        var entity = await _permissionRepo.FindAsync(key);
        if (entity == null)
        {
            return default;
        }
        Mapper.Map(dto, entity);
        _permissionRepo.Update(entity);
        await UnitOfWork.SaveChangesAsync();
        return Mapper.Map<MenuReadDto>(entity);
    }
}
