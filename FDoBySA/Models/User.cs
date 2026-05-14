using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDoBySA.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsFrozen { get; set; }
        public string FrozenReason { get; set; }
    }
}
