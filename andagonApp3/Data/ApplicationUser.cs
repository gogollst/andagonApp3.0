using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace andagonApp3.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public List<string> Roles { get; set; } = new() { ApplicationRoles.User };
    }

}
