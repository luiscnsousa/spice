

namespace Spice.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Utility;
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext db;

        public UserController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var otherUsers = await this.db.ApplicationUser.Where(u => u.Id != claim.Value).ToListAsync();

            return View(otherUsers);
        }

        public async Task<IActionResult> Lock(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.NotFound();
            }

            var applicationUser = await this.db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == id);
            if (applicationUser == null)
            {
                return this.NotFound();
            }

            applicationUser.LockoutEnd = DateTime.UtcNow.AddYears(1000);

            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }

        public async Task<IActionResult> Unlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.NotFound();
            }

            var applicationUser = await this.db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == id);
            if (applicationUser == null)
            {
                return this.NotFound();
            }

            applicationUser.LockoutEnd = DateTime.UtcNow;

            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }
    }
}