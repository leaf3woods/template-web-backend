using Template.Web.Application.Dtos;
using Template.Web.Domain.Entities.Authority;

namespace Template.Web.Application.Services.Base
{
    public interface IMenuService : 
        ICrudAppService<Permission, Guid, MenuReadDto, MenuQueryDto, MenuCreateDto, MenuUpdateDto>,
        IBaseService
    {
        Task<IEnumerable<MenuReadDto>> GetAllMenusAsync();

        Task<IEnumerable<MenuReadDto>> GetRootMenusAsTreeAsync();

        Task<int> SetMenuRouteAsync();
    }
}
