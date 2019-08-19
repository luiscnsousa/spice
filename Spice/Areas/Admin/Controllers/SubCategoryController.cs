namespace Spice.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using Spice.Models.ViewModels;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Area("Admin")]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext db;

        public SubCategoryController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [TempData]
        public string StatusMessage { get; set; }

        // GET - INDEX
        public async Task<IActionResult> Index()
        {
            var subCategories = await this.db.SubCategory.Include(s => s.Category).ToListAsync();

            return View(subCategories);
        }

        // GET - CREATE
        public async Task<IActionResult> Create()
        {
            var model = new SubCategoryAndCategoryViewModel
            {
                CategoryList = await this.db.Category.ToListAsync(),
                SubCategory = new SubCategory(),
                SubCategoryList = await this.db.SubCategory
                    .OrderBy(s => s.Name)
                    .Select(s => s.Name)
                    .Distinct()
                    .ToListAsync()
            };

            return this.View(model);
        }

        // POST - CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var doesSubCategoryExist = this.db.SubCategory
                    .Include(s => s.Category)
                    .Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);

                if (await doesSubCategoryExist.AnyAsync())
                {
                    this.StatusMessage =
                        $"Error: Sub Category exists under {(await doesSubCategoryExist.FirstAsync()).Category.Name} category. Please use another name.";
                }
                else
                {
                    this.db.SubCategory.Add(model.SubCategory);
                    await this.db.SaveChangesAsync();

                    return this.RedirectToAction(nameof(this.Index));
                }
            }

            var modelVM = new SubCategoryAndCategoryViewModel
            {
                CategoryList = await this.db.Category.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await this.db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync(),
                StatusMessage = this.StatusMessage
            };

            return this.View(modelVM);
        }

        [ActionName("GetSubCategory")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            var subCategories = await (from subCategory in this.db.SubCategory
                                       where subCategory.CategoryId == id
                                       select subCategory).ToListAsync();

            return Json(new SelectList(subCategories, "Id", "Name"));
        }

        // GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var subCategory = await this.db.SubCategory.SingleOrDefaultAsync(s => s.Id == id.Value);
            if (subCategory == null)
            {
                return this.NotFound();
            }

            var model = new SubCategoryAndCategoryViewModel
            {
                CategoryList = await this.db.Category.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await this.db.SubCategory
                    .OrderBy(s => s.Name)
                    .Select(s => s.Name)
                    .Distinct()
                    .ToListAsync()
            };

            return this.View(model);
        }

        // POST - EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryAndCategoryViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var doesSubCategoryExist = this.db.SubCategory
                    .Include(s => s.Category)
                    .Where(s => s.Name == model.SubCategory.Name && s.Category.Id == model.SubCategory.CategoryId);

                if (await doesSubCategoryExist.AnyAsync())
                {
                    this.StatusMessage =
                        $"Error: Sub Category exists under {(await doesSubCategoryExist.FirstAsync()).Category.Name} category. Please use another name.";
                }
                else
                {
                    var subCategory = await this.db.SubCategory.FindAsync(model.SubCategory.Id);
                    subCategory.Name = model.SubCategory.Name;

                    await this.db.SaveChangesAsync();

                    return this.RedirectToAction(nameof(this.Index));
                }
            }

            var modelVM = new SubCategoryAndCategoryViewModel
            {
                CategoryList = await this.db.Category.ToListAsync(),
                SubCategory = model.SubCategory,
                SubCategoryList = await this.db.SubCategory.OrderBy(s => s.Name).Select(s => s.Name).ToListAsync(),
                StatusMessage = this.StatusMessage
            };

            return this.View(modelVM);
        }

        // GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var subCategory = await this.db.SubCategory
                .Include(s => s.Category)
                .SingleOrDefaultAsync(s => s.Id == id.Value);

            if (subCategory == null)
            {
                return this.NotFound();
            }

            return this.View(subCategory);
        }

        // GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var subCategory = await this.db.SubCategory
                .Include(s => s.Category)
                .SingleOrDefaultAsync(s => s.Id == id.Value);
            if (subCategory == null)
            {
                return this.NotFound();
            }

            return this.View(subCategory);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var subCategory = await this.db.SubCategory.FindAsync(id);
            if (subCategory == null)
            {
                return this.View();
            }

            this.db.Remove(subCategory);
            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }
    }
}