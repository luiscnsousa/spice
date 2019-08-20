namespace Spice.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using Spice.Models.ViewModels;
    using Spice.Utility;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IHostingEnvironment hostingEnvironment;

        public MenuItemController(ApplicationDbContext db, IHostingEnvironment hostingEnvironment)
        {
            this.db = db;
            this.hostingEnvironment = hostingEnvironment;
            this.MenuItemVM = new MenuItemViewModel
            {
                Category = this.db.Category,
                MenuItem = new MenuItem()
            };
        }

        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }

        public async Task<IActionResult> Index()
        {
            var menuItems = await this.db.MenuItem
                .Include(m => m.Category)
                .Include(m => m.SubCategory)
                .ToListAsync();

            return View(menuItems);
        }

        // GET - CREATE
        public IActionResult Create()
        {
            return View(this.MenuItemVM);
        }

        // POST - CREATE
        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePOST()
        {
            this.MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!this.ModelState.IsValid)
            {
                return View(this.MenuItemVM);
            }

            this.db.MenuItem.Add(this.MenuItemVM.MenuItem);
            await this.db.SaveChangesAsync();

            // work on the image saving

            var webRootPath = this.hostingEnvironment.WebRootPath;
            var files = this.HttpContext.Request.Form.Files;

            var menuItemFromDB = await this.db.MenuItem.FindAsync(this.MenuItemVM.MenuItem.Id);

            if (files.Any())
            {
                // file has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var extension = Path.GetExtension(files[0].FileName);

                using (var fileStream = new FileStream(Path.Combine(uploads, this.MenuItemVM.MenuItem.Id + extension), FileMode.Create))
                {
                    await files[0].CopyToAsync(fileStream);
                }

                menuItemFromDB.Image = $@"\images\{this.MenuItemVM.MenuItem.Id}{extension}";
            }
            else
            {
                // no file was uploaded - use default
                var uploads = Path.Combine(webRootPath, $@"images\{SD.DefaultFoodImage}");
                System.IO.File.Copy(uploads, $@"{webRootPath}\images\{this.MenuItemVM.MenuItem.Id}.png");
                menuItemFromDB.Image = $@"\images\{this.MenuItemVM.MenuItem.Id}.png";
            }

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

            this.MenuItemVM.MenuItem = await this.db.MenuItem
                .Include(m => m.Category)
                .Include(m => m.SubCategory)
                .SingleOrDefaultAsync(m => m.Id == id.Value);

            this.MenuItemVM.SubCategory = await this.db.SubCategory
                .Where(s => s.CategoryId == this.MenuItemVM.MenuItem.CategoryId)
                .ToListAsync();

            if (this.MenuItemVM.MenuItem == null)
            {
                return this.NotFound();
            }

            return View(this.MenuItemVM);
        }

        // POST - EDIT
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            this.MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!this.ModelState.IsValid)
            {
                this.MenuItemVM.SubCategory = await this.db.SubCategory
                    .Where(s => s.CategoryId == this.MenuItemVM.MenuItem.CategoryId)
                    .ToListAsync();

                return View(this.MenuItemVM);
            }

            // work on the image saving

            var webRootPath = this.hostingEnvironment.WebRootPath;
            var files = this.HttpContext.Request.Form.Files;

            var menuItemFromDB = await this.db.MenuItem.FindAsync(this.MenuItemVM.MenuItem.Id);

            if (files.Any())
            {
                // file has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                var newExtension = Path.GetExtension(files[0].FileName);

                // delete the original file
                var imagePath = Path.Combine(webRootPath, menuItemFromDB.Image.TrimStart('\\'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                // upload the new file
                using (var fileStream = new FileStream(Path.Combine(uploads, this.MenuItemVM.MenuItem.Id + newExtension), FileMode.Create))
                {
                    await files[0].CopyToAsync(fileStream);
                }

                menuItemFromDB.Image = $@"\images\{this.MenuItemVM.MenuItem.Id}{newExtension}";
            }

            menuItemFromDB.Name = this.MenuItemVM.MenuItem.Name;
            menuItemFromDB.Description = this.MenuItemVM.MenuItem.Description;
            menuItemFromDB.Price = this.MenuItemVM.MenuItem.Price;
            menuItemFromDB.Spicyness = this.MenuItemVM.MenuItem.Spicyness;
            menuItemFromDB.Category = this.MenuItemVM.MenuItem.Category;
            menuItemFromDB.SubCategory = this.MenuItemVM.MenuItem.SubCategory;

            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }
    }
}