using Microsoft.AspNetCore.Identity;

namespace andagonApp3.Data
{
    public class RolePreservingLookupNormalizer : ILookupNormalizer
    {
        public string NormalizeName(string name) => name.ToUpperInvariant();

        public string NormalizeEmail(string email) => email.ToUpperInvariant();

        // Do not change role names; preserve original case
        public string Normalize(string key) => key;
    }
}
