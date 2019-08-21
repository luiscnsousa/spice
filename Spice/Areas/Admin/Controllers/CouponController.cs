namespace Spice.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext db;

        public CouponController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public async Task<IActionResult> Index()
        {
            var coupons = await this.db.Coupon.ToListAsync();

            return View(coupons);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(coupon);
            }

            var files = this.HttpContext.Request.Form.Files;
            if (files.Any())
            {
                byte[] p1 = null;
                using (var fs1 = files[0].OpenReadStream())
                {
                    using (var ms1 = new MemoryStream())
                    {
                        fs1.CopyTo(ms1);
                        p1 = ms1.ToArray();
                    }
                }

                coupon.Picture = p1;
            }

            this.db.Coupon.Add(coupon);
            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var coupon = await this.db.Coupon.FindAsync(id.Value);
            if (coupon == null)
            {
                return this.NotFound();
            }

            return this.View(coupon);
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(Coupon coupon)
        {
            if (coupon.Id == 0)
            {
                return this.NotFound();
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(coupon);
            }

            var couponFromDb = await this.db.Coupon.FindAsync(coupon.Id);
            couponFromDb.MinimumAmount = coupon.MinimumAmount;
            couponFromDb.Name = coupon.Name;
            couponFromDb.Discount = coupon.Discount;
            couponFromDb.CouponType = coupon.CouponType;
            couponFromDb.IsActive = coupon.IsActive;

            var files = this.HttpContext.Request.Form.Files;
            if (files.Any())
            {
                byte[] p1 = null;
                using (var fs1 = files[0].OpenReadStream())
                {
                    using (var ms1 = new MemoryStream())
                    {
                        fs1.CopyTo(ms1);
                        p1 = ms1.ToArray();
                    }
                }

                coupon.Picture = p1;
            }

            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var coupon = await this.db.Coupon.FindAsync(id.Value);
            if (coupon == null)
            {
                return this.NotFound();
            }

            return this.View(coupon);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var coupon = await this.db.Coupon.FindAsync(id.Value);
            if (coupon == null)
            {
                return this.NotFound();
            }

            return this.View(coupon);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            if (!id.HasValue)
            {
                return this.NotFound();
            }

            var coupon = await this.db.Coupon.FindAsync(id.Value);
            if (coupon == null)
            {
                return this.NotFound();
            }

            this.db.Coupon.Remove(coupon);
            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }
    }
}