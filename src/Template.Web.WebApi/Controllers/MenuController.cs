using Microsoft.AspNetCore.Mvc;
using Template.Web.Application.Dtos;
using Template.Web.Application.Services.Base;
using Template.Web.WebApi.Utilities;

namespace Template.Web.WebApi.Controllers
{
    /// <summary>
    ///     菜单
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        private readonly IMenuService _menuService;

        /// <summary>
        ///     获取树形菜单
        ///     auth: anonymous
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("tree")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ResponseWrapper<IEnumerable<MenuReadDto>>> GetRootMenusAsTree() =>
            (await _menuService.GetRootMenusAsTreeAsync()).Wrap();

        /// <summary>
        ///     删除菜单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("id/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ResponseWrapper<int>> DeleteMenu(Guid id) =>
            (await _menuService.DeleteAsync(id)).Wrap();

        /// <summary>
        ///     添加菜单
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ResponseWrapper<MenuReadDto>> CreateMenuAsync(MenuCreateDto dto) =>
            (await _menuService.CreateAsync(dto)).Wrap()!;
    }
}
