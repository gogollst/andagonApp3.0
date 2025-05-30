using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace andagonApp3.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public List<string> Roles { get; set; } = new List<string>();
        public string? AuthenticatorKey { get; set; }
        public string? FirstName { get; set; }
        public string? Name { get; set; }
        public List<RecoveryCode> RecoveryCodes { get; set; } = new List<RecoveryCode>();
    }
    public class RecoveryCode
    {
        public string Code { get; set; }
        public bool IsRedeemed { get; set; }
    }
}
