using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Liaro.Entities.Security;
using Liaro.DataLayer.Abstract;

namespace Liaro.ServiceLayer.Security
{
    public interface IRolesService
    {
        Task<List<Role>> FindUserRolesAsync(int userId);
        Task<bool> IsUserInRoleAsync(int userId, string roleName);
        Task<List<User>> FindUsersInRoleAsync(string roleName);
    }

    public class RolesService : IRolesService
    {
        private readonly IEntityBaseRepository<UserRole> _userRoles;


        public RolesService(IEntityBaseRepository<UserRole> userRoles)
        {
            _userRoles = userRoles;
        }

        public async Task<List<Role>> FindUserRolesAsync(int userId)
        {
            return await _userRoles.GetAllQueryable()
                            .Include(x => x.Role)
                            .AsNoTracking()
                            .Where(x => x.UserId == userId)
                            .Select(x => x.Role)
                            .OrderBy(x => x.Name)
                            .ToListAsync();

            // var userRolesQuery = from role in _roles
            //                      from userRoles in role.UserRoles
            //                      where userRoles.UserId == userId
            //                      select role;

            // return userRolesQuery.OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<bool> IsUserInRoleAsync(int userId, string roleName)
        {
            return await _userRoles.GetAllQueryable()
                                .Include(x => x.Role)
                                .AsNoTracking()
                                .AnyAsync(x => x.UserId == userId && x.Role.Name == roleName);

            // var userRolesQuery = from role in _roles
            //                      where role.Name == roleName
            //                      from user in role.UserRoles
            //                      where user.UserId == userId
            //                      select role;
            // var userRole = await userRolesQuery.FirstOrDefaultAsync();
            // return userRole != null;
        }

        public async Task<List<User>> FindUsersInRoleAsync(string roleName)
        {
            return await _userRoles.GetAllQueryable()
                                .AsNoTracking()
                                .Include(x => x.User)
                                .Include(x => x.Role)
                                .Where(x => x.Role.Name == roleName)
                                .Select(x => x.User)
                                .ToListAsync();

            // var roleUserIdsQuery = from role in _roles
            //                        where role.Name == roleName
            //                        from user in role.UserRoles
            //                        select user.UserId;
            // return _users.Where(user => roleUserIdsQuery.Contains(user.Id))
            //             .ToListAsync();
        }
    }
}