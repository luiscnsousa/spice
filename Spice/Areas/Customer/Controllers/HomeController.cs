namespace Spice.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using Spice.Models.ViewModels;
    using System.Diagnostics;
    using System.Linq;
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

            return View(indexViewModel);
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
