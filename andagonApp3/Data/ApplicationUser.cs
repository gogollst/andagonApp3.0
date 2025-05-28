using Microsoft.AspNetCore.Identity;

namespace andagonApp3.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Indicates whether the user account is active. When false, the user
        /// is not allowed to sign in.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

}
