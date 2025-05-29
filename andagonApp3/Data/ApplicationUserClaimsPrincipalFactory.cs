using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace andagonApp3.Data
{
    /// <summary>
    /// Ensures that role claims are populated from the ApplicationUser's role list.
    /// </summary>
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            if (user.Roles is not null)
            {
                foreach (var role in user.Roles)
                {
                    if (!identity.HasClaim(Options.ClaimsIdentity.RoleClaimType, role))
                    {
                        identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, role));
                    }
                }
            }
            return identity;
        }
    }
}
