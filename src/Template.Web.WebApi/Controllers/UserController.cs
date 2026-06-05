using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Web.Application.Auth;
using Template.Web.Application.Dtos;
using Template.Web.Application.Services.Base;
using Template.Web.WebApi.Utilities;

namespace Template.Web.WebApi.Controllers
{
    /// <summary>
    ///     用户资源
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Policy = ManagedResource.User)]
    [ApiController]
    public class UserController : ControllerBase
    {
        /// <summary>
        ///     注入服务
        /// </summary>
        /// <param name="userService">用户服务</param>
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private readonly IUserService _userService;

        /// <summary>
        ///     获取指定Id的用户
        ///     auth: super
        /// </summary>
        /// <param name="userId">用户guid</param>
        /// <returns>用户详情</returns>
        [HttpGet]
        [Authorize]
        [Route("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Policy = $"{ManagedResource.User}.{ManagedAction.Get}.Id")]
        public async Task<ResponseWrapper<UserReadDto?>> GetUser(Guid userId) =>
            (await _userService.GetUserAsync(userId)).Wrap();

        /// <summary>
        ///     模糊匹配用户名和昵称
        ///     auth: super
        /// </summary>
        /// <param name="name">用户名或昵称</param>
        /// <returns>匹配的用户列表</returns>
        [HttpGet]
        [Route("where")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Policy = $"{ManagedResource.User}.{ManagedAction.Get}.Query")]
        public async Task<ResponseWrapper<IEnumerable<UserReadDto>>> GetUsers(
            string? name = null
        ) => (await _userService.GetUsersWhereAsync(name)).Wrap();

        /// <summary>
        ///     删除用户
        ///     auth: super
        /// </summary>
        /// <param name="userId">用户guid</param>
        /// <returns>已删除用户数</returns>
        [HttpDelete]
        [Route("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Policy = $"{ManagedResource.User}.{ManagedAction.Delete}.Id")]
        public async Task<ResponseWrapper<int>> Delete(Guid userId) =>
            (await _userService.DeleteAsync(userId)).Wrap();

        /// <summary>
        ///     切换角色
        ///     auth: super
        /// </summary>
        /// <param name="userId">用户guid</param>
        /// <param name="roleIds">角色guid</param>
        /// <returns>用户更改后的详情</returns>
        [HttpPost]
        [Route("{userId:guid}/role/{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Policy = $"{ManagedResource.User}.{ManagedAction.Put}.Role")]
        public async Task<ResponseWrapper<UserReadDto?>> ModifyRole(
            Guid userId,
            IEnumerable<Guid> roleIds
        ) => (await _userService.ChangeRoleAsync(userId, roleIds)).Wrap();

        /// <summary>
        ///     更换自己的密码
        ///     auth: anonymous
        /// </summary>
        /// <param name="passwordDto">用于验证用户身份</param>
        /// <returns>更换密码状态</returns>
        [HttpPut]
        [Route("pwd")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ResponseWrapper<int>> ChangePassword(ChangePasswordDto passwordDto) =>
            (await _userService.ChangePasswordAsync(passwordDto)).Wrap();

        /// <summary>
        ///     重置某个用户的密码
        ///     auth: admin
        /// </summary>
        /// <param name="userId">用户guid</param>
        /// <returns>重置状态</returns>
        [HttpPut]
        [Route("{userId}/pwd")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Authorize(Policy = $"{ManagedResource.User}.{ManagedAction.Put}.ResetPwd")]
        public async Task<ResponseWrapper<int>> ResetPassword(Guid userId) =>
            (await _userService.ResetPasswordAsync(userId)).Wrap();
    }
}
