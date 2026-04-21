using Template.Web.Application.Dtos;
using Template.Web.Application.Services.Base;
using Template.Web.WebApi.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Template.Web.WebApi.Controllers
{
    /// <summary>
    ///     主页
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        public HomeController(
                IUserService userService
            )
        {
            _userService = userService;
        }

        private readonly IUserService _userService;

        /// <summary>
        ///     获取验证码
        ///     auth: anonymous
        /// </summary>
        /// <returns>base64验证码图片</returns>
        [HttpGet]
        [Route("captcha")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ResponseWrapper<CaptchaReadDto?>> GetCaptcha() =>
            (await _userService.GenerateCaptchaAsync()).Wrap();

        /// <summary>
        ///     用户登录
        ///     auth: anonymous
        /// </summary>
        /// <param name="credential">登录验证内容</param>
        /// <returns>登录状态</returns>
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ResponseWrapper<string>> Login(UserLoginDto credential) =>
            (await _userService.LoginAsync(credential)).Wrap()!;

        /// <summary>
        ///     用户登出
        ///     auth: user
        /// </summary>
        /// <returns>void</returns>
        [HttpPost]
        [Route("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task Logout() => await _userService.LogoutAsync(HttpContext.User.Claims);

        /// <summary>
        ///     用户注册
        ///     auth: anonymous
        /// </summary>
        /// <param name="registerDto">注册必要字段</param>
        /// <returns>成功注册的用户/null</returns>
        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ResponseWrapper<UserReadDto?>> Register(UserRegisterDto registerDto) =>
            (await _userService.RegisterAsync(registerDto)).Wrap();
    }
}