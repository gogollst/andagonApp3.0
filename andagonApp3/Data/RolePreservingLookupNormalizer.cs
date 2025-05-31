using Microsoft.AspNetCore.Identity;

namespace andagonApp3.Data
{
    /// <summary>
    /// Keeps role names in their original casing while still normalizing emails.
    /// </summary>
    public class RolePreservingLookupNormalizer : ILookupNormalizer
    {
        public string NormalizeEmail(string email)
            => email.ToUpperInvariant();

        public string NormalizeName(string name)
            => name;
    }
}
