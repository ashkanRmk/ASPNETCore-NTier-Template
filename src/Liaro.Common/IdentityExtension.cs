using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Liaro.Common
{
    public static class IdentityExtension
    {
        public static int UserId(this ClaimsPrincipal user)
        {
            try
            {
                if (user.Identity.IsAuthenticated)
                {
                    var sub = user.FindFirst(ClaimTypes.UserData)?.Value;
                    return int.Parse(sub);
                }
                else
                    return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static List<string> Roles(this ClaimsPrincipal user)
        {
            if (user.Identity.IsAuthenticated)
            {
                var claimsIdentity = user.Identity as ClaimsIdentity;
                return claimsIdentity.Claims.Where(x => x.Type == ClaimTypes.Role)
                                                .Select(x => x.Value)
                                                .ToList();
            }
            else
                return null;
        }

        public static string UserName(this ClaimsPrincipal user)
        {
            return user.Identity.Name;
        }
    }
}