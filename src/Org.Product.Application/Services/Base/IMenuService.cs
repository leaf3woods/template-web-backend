using Org.Product.Application.Dtos;
using Org.Product.Domain.Entities.Authority;

namespace Org.Product.Application.Services.Base;

public interface IMenuService
    : ICrudAppService<
        Permission,
        Guid,
        MenuReadDto,
        MenuQueryDto,
        MenuCreateDto,
        MenuUpdateDto
    >,
        IBaseService
{
    Task<IEnumerable<MenuReadDto>> GetAllMenusAsync();

    Task<IEnumerable<MenuReadDto>> GetRootMenusAsTreeAsync();

    Task<int> SetMenuRouteAsync();
}
