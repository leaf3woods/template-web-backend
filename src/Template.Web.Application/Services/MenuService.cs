using Microsoft.EntityFrameworkCore;
using Template.Web.Application.Dtos;
using Template.Web.Application.Services.Base;
using Template.Web.Core;
using Template.Web.Core.Exceptions;
using Template.Web.Domain.Entities.Authority;
using Template.Web.Infrastructure.DbContexts;
using Template.Web.Infrastructure.Utilities;

namespace Template.Web.Application.Services
{
    public class MenuService : CrudAppService<Permission, Guid, MenuReadDto, MenuQueryDto, MenuCreateDto, MenuUpdateDto>, IMenuService
    {
        public MenuService(ApiDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public override async Task<IEnumerable<MenuReadDto>> GetListAsync(MenuQueryDto? queryDto = null)
        {
            var permissions = await Queryable.ToListAsync();
            return Mapper.Map<IEnumerable<MenuReadDto>>(permissions);
        }

        public override async Task<PaginatedList<MenuReadDto>> GetPaginatedListAsync(MenuQueryDto queryDto)
        {
            var permissions = await Queryable
                .WhereIf(!string.IsNullOrEmpty(queryDto.Name), x => x.Name.Contains(queryDto.Name!))
                .WhereIf(!string.IsNullOrEmpty(queryDto.Code), x => x.Code.Contains(queryDto.Code!))
                .WhereIf(queryDto.ParentId != null, x => x.ParentId == queryDto.ParentId)
                .WhereIf(queryDto.Type != null, x => x.Type == (PermissionType)queryDto.Type.Value)
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
            var menu = await DbContext.Permissions.FindAsync(key) ?? throw new NotFoundException("id not exist");

            if (menu.ParentId is null) throw new NotAcceptableException("root menu can't delete");

            if (await DbContext.Permissions.AnyAsync(m => m.ParentId == key))
            {
                throw new NotAcceptableException("child menu exist");
            }

            DbContext.Permissions.Remove(menu);
            return await DbContext.SaveChangesAsync();
        }

        public override async Task<MenuReadDto?> UpdateAsync(Guid key, MenuUpdateDto dto)
        {
            var entity = await DbContext.Permissions.FindAsync(key);
            if (entity == null)
            {
                return default;
            }
            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.Description = dto.Description;
            entity.ParentId = dto.ParentId;
            entity.Type = (PermissionType)dto.Type;
            entity.Order = dto.Order;
            entity.IconUrl = dto.IconUrl;
            entity.Route = dto.Route;
            entity.Visible = dto.Visible;
            entity.Favorite = dto.Favorite;
            entity.State = dto.State;
            DbContext.Permissions.Update(entity);
            await DbContext.SaveChangesAsync();
            return Mapper.Map<MenuReadDto>(entity);
        }
    }
}