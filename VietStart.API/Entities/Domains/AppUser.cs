using Microsoft.AspNetCore.Identity;

namespace VietStart.API.Entities.Domains
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }

    }
}
