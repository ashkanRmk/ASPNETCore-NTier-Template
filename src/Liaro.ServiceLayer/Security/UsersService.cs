using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Liaro.Common;
using Liaro.DataLayer.Abstract;
using Liaro.Entities.Security;
using Liaro.ModelLayer;
using Liaro.ServiceLayer.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Liaro.ServiceLayer.Security
{
    public interface IUsersService
    {
        Task<string> GetSerialNumberAsync(int userId);
        Task<User> FindUserAsync(string username, string password);
        Task<User> FindUserAsync(int userId);
        Task<User> FindUserByMobile(string mobile);
        Task UpdateUserLastActivityDateAsync(int userId);
        Task<User> GetCurrentUserAsync();
        int GetCurrentUserId();
        Task<(bool Succeeded, string Error)> ChangePasswordAsync(User user, string currentPassword, string newPassword);
        Task<LoginByMobileResultVM> SetUserMobileLoginData(string mobile);
        Task<LoginByMobileResultVM> CheckAuthCode(string mobile, string code);
    }

    public class UsersService : IUsersService
    {
        private readonly IEntityBaseRepository<User> _users;
        private readonly ISecurityService _securityService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IKavenegarService _kavenegarService;

        public UsersService(
            IEntityBaseRepository<User> users,
            ISecurityService securityService,
            IKavenegarService kavenegarService,
            IHttpContextAccessor contextAccessor)
        {
            _users = users;
            _kavenegarService = kavenegarService;

            _securityService = securityService;
            _securityService.CheckArgumentIsNull(nameof(_securityService));

            _contextAccessor = contextAccessor;
            _contextAccessor.CheckArgumentIsNull(nameof(_contextAccessor));
        }

        public Task<User> FindUserAsync(int userId)
        {
            return _users.GetAllQueryable().Where(x => x.Id == userId).FirstOrDefaultAsync();
        }

        public Task<User> FindUserByMobile(string mobile)
        {
            return _users.GetAllQueryable()
                        .Where(x => x.Mobile == mobile)
                        .FirstOrDefaultAsync();
        }

        public Task<User> FindUserAsync(string username, string password)
        {
            var passwordHash = _securityService.GetSha256Hash(password);
            return _users.GetAllQueryable()
                        .Where(x => (x.Username.ToLower() == username.ToLower()
                                    || x.Email.ToLower() == username.ToLower()
                                    || x.Mobile == username)
                                && x.Password == passwordHash)
                        .FirstOrDefaultAsync();
        }

        public async Task<string> GetSerialNumberAsync(int userId)
        {
            var user = await FindUserAsync(userId);
            return user.SerialNumber;
        }

        public async Task UpdateUserLastActivityDateAsync(int userId)
        {
            var user = await FindUserAsync(userId);
            if (user.LastLoggedIn != null)
            {
                var updateLastActivityDate = TimeSpan.FromMinutes(2);
                var currentUtc = DateTimeOffset.UtcNow;
                var timeElapsed = currentUtc.Subtract(user.LastLoggedIn.Value);
                if (timeElapsed < updateLastActivityDate)
                {
                    return;
                }
            }
            user.LastLoggedIn = DateTimeOffset.UtcNow;
            _users.Update(user);
            await _users.CommitAsync();
        }

        public async Task<LoginByMobileResultVM> SetUserMobileLoginData(string mobile)
        {
            var user = await FindUserByMobile(mobile);
            var result = new LoginByMobileResultVM();
            if (user == null || !user.IsActive)
            {
                result.Error = "کاربری با این شماره موبایل یافت نشد!";
                return result;
            }
            if (user.MobileLoginExpire != null && user.MobileLoginExpire > DateTimeOffset.UtcNow)
            {
                result.Error = "لطفا ۵ دقیقه دیگر دوباره امتحان کنید.";
                return result;
            }
            user.MobileLoginExpire = DateTimeOffset.UtcNow.AddMinutes(5);
            user.LoginCode = StringUtils.GetUniqueKey(6, true);

            var res = await _kavenegarService.SendLoginCode(user.LoginCode, mobile, user.DisplayName);
            if (res == null || res.@return.status != 200)
            {
                result.Error = "مشکلی هنگام ارسال پیامک رخ داده است!";
                return result;
            }
            _users.Update(user);
            await _users.CommitAsync();

            return result;
        }

        public async Task<LoginByMobileResultVM> CheckAuthCode(string mobile, string code)
        {
            var user = await _users.GetAllQueryable()
                            .Where(x => x.Mobile == mobile && x.LoginCode == code)
                            .FirstOrDefaultAsync();
            var result = new LoginByMobileResultVM();
            if (user == null || !user.IsActive)
            {
                result.Error = "کد وارد شده اشتباه است!";
                return result;
            }
            if (user.MobileLoginExpire != null && user.MobileLoginExpire < DateTimeOffset.UtcNow)
            {
                result.Error = "کد ارسالی منقضی شده است، مجدد کد جدید دریافت کنید.";
                return result;
            }
            user.MobileLoginExpire = null;
            user.LoginCode = null;

            _users.Update(user);
            await _users.CommitAsync();
            return result;
        }

        public int GetCurrentUserId()
        {
            var claimsIdentity = _contextAccessor.HttpContext.User.Identity as ClaimsIdentity;
            var userDataClaim = claimsIdentity?.FindFirst(ClaimTypes.UserData);
            var userId = userDataClaim?.Value;
            return string.IsNullOrWhiteSpace(userId) ? 0 : int.Parse(userId);
        }

        public Task<User> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            return FindUserAsync(userId);
        }

        public async Task<(bool Succeeded, string Error)> ChangePasswordAsync(User user, string currentPassword, string newPassword)
        {
            var currentPasswordHash = _securityService.GetSha256Hash(currentPassword);
            if (user.Password != currentPasswordHash)
            {
                return (false, "پسورد قبلی اشتباه وارد شده است.");
            }

            user.Password = _securityService.GetSha256Hash(newPassword);
            user.SerialNumber = Guid.NewGuid().ToString("N"); // To force other logins to expire.
            _users.Update(user);
            await _users.CommitAsync();
            return (true, string.Empty);
        }
    }
}
