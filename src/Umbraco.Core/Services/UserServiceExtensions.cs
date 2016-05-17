using System;
using System.Linq;
using Umbraco.Core.Models.Membership;

namespace Umbraco.Core.Services
{
    internal static class UserServiceExtensions
    {
        public static EntityPermission GetPermissions(this IUserService userService, IUser user, string path)
        {
            var ids = path.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.TryConvertTo<int>())
                .Where(x => x.Success)
                .Select(x => x.Result)
                .ToArray();
            if (ids.Length == 0) throw new InvalidOperationException("The path: " + path + " could not be parsed into an array of integers or the path was empty");

            return userService.GetPermissions(user, ids[ids.Length - 1]).FirstOrDefault();
        }

        /// <summary>
        /// Remove all permissions for this user for all nodes specified
        /// </summary>
        /// <param name="userService"></param>
        /// <param name="userId"></param>
        /// <param name="entityIds"></param>
        public static void RemoveUserPermissions(this IUserService userService, int userId, params int[] entityIds)
        {
            userService.ReplaceUserPermissions(userId, new char[] {}, entityIds);
        }

        /// <summary>
        /// Remove all permissions for this user for all nodes
        /// </summary>
        /// <param name="userService"></param>
        /// <param name="userId"></param>
        public static void RemoveUserPermissions(this IUserService userService, int userId)
        {
            userService.ReplaceUserPermissions(userId, new char[] { });
        }

    }
}