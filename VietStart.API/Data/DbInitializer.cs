using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VietStart.API.Entities.Domains;
using System.Threading.Tasks;

namespace VietStart.API.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            AppDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 🔹 Đảm bảo DB được tạo
            await context.Database.MigrateAsync();

            // 🔹 Tạo các role mặc định nếu chưa có
            string[] roles = new[] { "Admin", "Client" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 🔹 Tạo tài khoản Admin mặc định nếu chưa có
            var adminEmail = "admin@vietstart.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator",
                    Location = "Đà Nẵng"
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin@12345");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}
