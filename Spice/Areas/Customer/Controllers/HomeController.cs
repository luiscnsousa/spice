namespace Spice.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using Spice.Models.ViewModels;
    using Spice.Utility;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db;

        public HomeController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<IActionResult> Index()
        {
            var indexViewModel = new IndexViewModel
            {
                MenuItem = await this.db.MenuItem
                    .Include(m => m.Category)
                    .Include(m => m.SubCategory)
                    .ToListAsync(),
                Category = await this.db.Category.ToListAsync(),
                Coupon = await this.db.Coupon.Where(c => c.IsActive).ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                var count = await this.db.ShoppingCart
                    .Where(u => u.ApplicationUserId == claim.Value)
                    .CountAsync();

                this.HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);
            }

            return View(indexViewModel);
        }

        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var menuItemFromDb = await this.db.MenuItem
                .Include(m => m.Category)
                .Include(m => m.SubCategory)
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();

            var cart = new ShoppingCart
            {
                MenuItem = menuItemFromDb,
                MenuItemId = menuItemFromDb.Id
            };

            return this.View(cart);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(ShoppingCart cart)
        {
            cart.Id = 0;
            if (!this.ModelState.IsValid)
            {
                var menuItemFromDb = await this.db.MenuItem
                    .Include(m => m.Category)
                    .Include(m => m.SubCategory)
                    .Where(m => m.Id == cart.MenuItemId)
                    .FirstOrDefaultAsync();

                var validCart = new ShoppingCart
                {
                    MenuItem = menuItemFromDb,
                    MenuItemId = menuItemFromDb.Id
                };

                return this.View(validCart);
            }

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            cart.ApplicationUserId = claim.Value;

            var cartFromDb = await this.db.ShoppingCart
                .Where(c => c.ApplicationUserId == cart.ApplicationUserId && c.MenuItemId == cart.MenuItemId)
                .FirstOrDefaultAsync();

            if (cartFromDb == null)
            {
                await this.db.ShoppingCart.AddAsync(cart);
            }
            else
            {
                cartFromDb.Count += cart.Count;
            }

            await this.db.SaveChangesAsync();

            var count = await this.db.ShoppingCart
                .Where(c => c.ApplicationUserId == cart.ApplicationUserId)
                .CountAsync();

            this.HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);

            return this.RedirectToAction(nameof(this.Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
