using System.Linq;
using FDoBySA;

namespace FDoBySA.Helpers
{
    public static class UserSession
    {
        public static Users CurrentUser { get; set; }

        public static bool IsAuthenticated => CurrentUser != null;

        public static bool IsAdmin => CurrentUser?.RoleId == 3;

        public static bool IsAuthor => CurrentUser?.RoleId == 2;

        public static bool IsReader => CurrentUser?.RoleId == 1;

        public static bool IsFrozen => CurrentUser?.IsFrozen == true;

        public static string FrozenReason => CurrentUser?.FrozenReason ?? "";

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static void RefreshUser()
        {
            if (CurrentUser != null)
            {
                CurrentUser = Core.Context.Users
                    .FirstOrDefault(u => u.UserId == CurrentUser.UserId);
            }
        }
    }
}