namespace Spice.ViewComponents
{
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;

    public class UserNameViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext db;

        public UserNameViewComponent(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claisIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claisIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var userFromDb = await this.db.ApplicationUser
                .FirstOrDefaultAsync(u => u.Id == claim.Value);

            return this.View(userFromDb);
        }
    }
}
