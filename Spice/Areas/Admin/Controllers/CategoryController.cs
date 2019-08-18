namespace Spice.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using System.Threading.Tasks;

    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext db;

        public CategoryController(ApplicationDbContext db)
        {
            this.db = db;
        }

        // GET action method
        public async Task<IActionResult> Index()
        {
            return View(await this.db.Category.ToListAsync());
        }
    }
}