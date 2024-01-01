using System.Threading.Tasks;
using Liaro.Common;
using Liaro.ModelLayer;
using Liaro.ModelLayer.Security;
using Liaro.ServiceLayer.Contracts;
using Liaro.ServiceLayer.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Liaro.Controllers.APIs
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly IUsersService _usersService;
        private readonly ITokenStoreService _tokenStoreService;
        private readonly IAntiForgeryCookieService _antiforgery;
        private readonly ITokenFactoryService _tokenFactoryService;
        private readonly IKavenegarService _kavenegarService;

        public AccountController(
            IUsersService usersService,
            ITokenStoreService tokenStoreService,
            ITokenFactoryService tokenFactoryService,
            IKavenegarService kavenegarService,
            IAntiForgeryCookieService antiforgery)
        {
            _usersService = usersService;
            _usersService.CheckArgumentIsNull(nameof(usersService));

            _tokenStoreService = tokenStoreService;
            _tokenStoreService.CheckArgumentIsNull(nameof(tokenStoreService));

            _antiforgery = antiforgery;
            _antiforgery.CheckArgumentIsNull(nameof(antiforgery));

            _tokenFactoryService = tokenFactoryService;
            _tokenFactoryService.CheckArgumentIsNull(nameof(tokenFactoryService));

            _kavenegarService = kavenegarService;
            _kavenegarService.CheckArgumentIsNull(nameof(kavenegarService));
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody]LoginVM loginUser)
        {
            if (loginUser == null)
            {
                return BadRequest("user is not set.");
            }

            var user = await _usersService.FindUserAsync(loginUser.Username, loginUser.Password);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("Error", "نام کاربری یا پسورد اشتباه وارد شده.");
                return BadRequest(ModelState.DictionaryErrors());
            }

            var result = await _tokenFactoryService.CreateJwtTokensAsync(user);
            await _tokenStoreService.AddUserTokenAsync(user, result.RefreshTokenSerial, result.AccessToken, null);

            _antiforgery.RegenerateAntiForgeryCookies(result.Claims);

            return Ok(new { access_token = result.AccessToken, refresh_token = result.RefreshToken });
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> LoginByMobileInit([FromBody]LoginByMobileVM model)
        {
            if (string.IsNullOrEmpty(model.Mobile) || !StringUtils.IsValidPhone(model.Mobile))
            {
                return BadRequest("لطفا شماره موبایل معتبر وارد کنید.");
            }
            //Check Expire time and send auth code via sms
            var codeRes = await _usersService.SetUserMobileLoginData(model.Mobile);
            if (!codeRes.IsSuccess)
            {
                ModelState.AddModelError("Error", codeRes.Error);
                return BadRequest(ModelState.DictionaryErrors());
            }

            return Ok();
        }

        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> LoginByMobile([FromBody]LoginByMobileVM model)
        {
            var codeRes = await _usersService.CheckAuthCode(model.Mobile, model.Code);
            if (!codeRes.IsSuccess)
            {
                ModelState.AddModelError("Error", codeRes.Error);
                return BadRequest(ModelState.DictionaryErrors());
            }
            var user = await _usersService.FindUserByMobile(model.Mobile);

            var result = await _tokenFactoryService.CreateJwtTokensAsync(user);
            await _tokenStoreService.AddUserTokenAsync(user, result.RefreshTokenSerial, result.AccessToken, null);

            _antiforgery.RegenerateAntiForgeryCookies(result.Claims);

            return Ok(new { access_token = result.AccessToken, refresh_token = result.RefreshToken });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RefreshToken([FromBody]JToken jsonBody)
        {
            var refreshTokenValue = jsonBody.Value<string>("refreshToken");
            if (string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                return BadRequest("refreshToken is not set.");
            }

            var token = await _tokenStoreService.FindTokenAsync(refreshTokenValue);
            if (token == null)
            {
                return Unauthorized();
            }

            var result = await _tokenFactoryService.CreateJwtTokensAsync(token.User);
            await _tokenStoreService.AddUserTokenAsync(token.User, result.RefreshTokenSerial, result.AccessToken, _tokenFactoryService.GetRefreshTokenSerial(refreshTokenValue));

            _antiforgery.RegenerateAntiForgeryCookies(result.Claims);

            return Ok(new { access_token = result.AccessToken, refresh_token = result.RefreshToken });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<bool> Logout(string refreshToken)
        {
            var userId = User.UserId();

            // The Jwt implementation does not support "revoke OAuth token" (logout) by design.
            // Delete the user's tokens from the database (revoke its bearer token)
            await _tokenStoreService.RevokeUserBearerTokensAsync(userId.ToString(), refreshToken);

            _antiforgery.DeleteAntiForgeryCookies();

            return true;
        }

        [HttpGet]
        public bool IsAuthenticated()
        {
            return User.Identity.IsAuthenticated;
        }

        [HttpGet]
        public IActionResult GetUserInfo()
        {
            return Ok(new
            {
                UserId = User.UserId(),
                Username = User.UserName(),
                Roles = User.Roles()
            });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _usersService.GetCurrentUserAsync();
            if (user == null)
            {
                return BadRequest("NotFound");
            }

            var result = await _usersService.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(result.Error);
        }
    }
}