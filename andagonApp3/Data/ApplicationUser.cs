using Microsoft.AspNetCore.Identity;
using DataBaseManager;
using System;
using System.Collections.Generic;

namespace andagonApp3.Data
{
    /// <summary>
    /// Application user stored in MongoDB.
    /// Implements IDocument so it can be managed by DBManager.
    /// </summary>
    public class ApplicationUser : IdentityUser, IDocument
    {
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// External login providers associated with this user.
        /// </summary>
        public List<ApplicationUserLogin> Logins { get; set; } = [];
    }

    /// <summary>
    /// Represent external login information stored with the user.
    /// </summary>
    public class ApplicationUserLogin
    {
        public string LoginProvider { get; set; } = string.Empty;
        public string ProviderKey { get; set; } = string.Empty;
        public string ProviderDisplayName { get; set; } = string.Empty;
    }
}
