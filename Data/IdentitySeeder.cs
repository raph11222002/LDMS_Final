using LDMS_Final;
using LDMS_Final.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Data
{
    public static class IdentitySeeder
    {
        public static async Task InitializeAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var config = services.GetRequiredService<IConfiguration>();

            await db.Database.MigrateAsync();

            string[] roles =
            [
                RoleNames.SuperAdmin,
                RoleNames.Admin,
                RoleNames.LogisticStaff,
                RoleNames.WarehouseStaff,
                RoleNames.Driver,
                RoleNames.Buyer,
            ];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var superUserName = config["SeedAccounts:SuperAdminUserName"] ?? "superadmin";
            var superEmail = config["SeedAccounts:SuperAdminEmail"] ?? "superadmin@ldms.com";
            var superPassword = config["SeedAccounts:SuperAdminPassword"] ?? "Super@12345";
            var superFullName = config["SeedAccounts:SuperAdminFullName"] ?? "System Super Admin";

            var superAdmin = await userManager.FindByNameAsync(superUserName)
                            ?? await userManager.FindByEmailAsync(superEmail);

            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    UserName = superUserName,
                    Email = superEmail,
                    FullName = superFullName,
                    ContactNumber = "09999080422",
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(superAdmin, superPassword);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(errors);
                }

                await userManager.AddToRoleAsync(superAdmin, RoleNames.SuperAdmin);
            }
            else if (!await userManager.IsInRoleAsync(superAdmin, RoleNames.SuperAdmin))
            {
                await userManager.AddToRoleAsync(superAdmin, RoleNames.SuperAdmin);
            }
        }
    }
}