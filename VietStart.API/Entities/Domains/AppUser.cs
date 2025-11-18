using Microsoft.AspNetCore.Identity;

namespace VietStart.API.Entities.Domains
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? Location { get; set; }
        public string? Bio { get; set; }
        public string? Avatar { get; set; }
        public DateTime? DOB { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? DeletedBy { get; set; }

        public ICollection<StartUp> StartUps { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Share> Shares { get; set; }
    }
}
