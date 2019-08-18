namespace Spice.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
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
            return this.View(await this.db.Category.ToListAsync());
        }

        // GET - CREATE
        public IActionResult Create()
        {
            return this.View();
        }

        // POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(category);
            }

            this.db.Category.Add(category);
            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }

        // GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var category = await this.db.Category.FindAsync(id.Value);
            if (category == null)
            {
                return this.NotFound();
            }

            return this.View(category);
        }

        // POST - EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(category);
            }

            this.db.Update(category);
            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }

        // GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var category = await this.db.Category.FindAsync(id.Value);
            if (category == null)
            {
                return this.NotFound();
            }

            return this.View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var category = await this.db.Category.FindAsync(id);

            if (category == null)
            {
                return this.View();
            }

            this.db.Remove(category);
            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }

        // GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var category = await this.db.Category.FindAsync(id.Value);
            if (category == null)
            {
                return this.NotFound();
            }

            return this.View(category);
        }
    }
}