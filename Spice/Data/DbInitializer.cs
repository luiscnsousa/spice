namespace Spice.Data
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Spice.Models;
    using Spice.Utility;

    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public DbInitializer(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            this.db = db;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public async Task InitializeAsync()
        {
            try
            {
                if ((await this.db.Database.GetPendingMigrationsAsync()).Any())
                {
                    await this.db.Database.MigrateAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if (await this.db.Roles.AnyAsync(r => r.Name == SD.ManagerUser))
            {
                return;
            }

            await this.roleManager.CreateAsync(new IdentityRole(SD.FrontDeskUser));
            await this.roleManager.CreateAsync(new IdentityRole(SD.KitchenUser));
            await this.roleManager.CreateAsync(new IdentityRole(SD.CustomerEndUser));
            await this.roleManager.CreateAsync(new IdentityRole(SD.ManagerUser));

            await this.userManager.CreateAsync(new ApplicationUser
            {
                UserName = "luiscnsousa@outlook.com",
                Email = "luiscnsousa@outlook.com",
                Name = "Luis Sousa",
                EmailConfirmed = true,
                PhoneNumber = "11223344"
            }, "Pa$$w0rd");

            var user = await this.db.Users.FirstOrDefaultAsync(u => u.Email == "luiscnsousa@outlook.com");

            await this.userManager.AddToRoleAsync(user, SD.ManagerUser);
        }
    }
}
