namespace Spice.Areas.Customer.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using Spice.Models.ViewModels;
    using Spice.Utility;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext db;

        public CartController(ApplicationDbContext db)
        {
            this.db = db;
        }

        [BindProperty]
        public OrderDetailsCart DetailCart { get; set; }

        public async Task<IActionResult> Index()
        {
            this.DetailCart = new OrderDetailsCart
            {
                OrderHeader = new OrderHeader()
            };

            this.DetailCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            this.DetailCart.ListCart = await this.db.ShoppingCart
                .Where(c => c.ApplicationUserId == claim.Value)
                .ToListAsync() ?? new List<ShoppingCart>();

            foreach (var list in this.DetailCart.ListCart)
            {
                list.MenuItem = await this.db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                this.DetailCart.OrderHeader.OrderTotal += list.MenuItem.Price * list.Count;
                list.MenuItem.Description = SD.ConvertToRawHtml(list.MenuItem.Description);

                if (list.MenuItem.Description.Length > 100)
                {
                    list.MenuItem.Description = $"{list.MenuItem.Description.Substring(0, 99)}...";
                }
            }

            this.DetailCart.OrderHeader.OrderTotalOriginal = this.DetailCart.OrderHeader.OrderTotal;

            var couponCode = this.HttpContext.Session.GetString(SD.ssCouponCode);
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                this.DetailCart.OrderHeader.CouponCode = couponCode;

                var couponFromDb = await this.db.Coupon
                    .FirstOrDefaultAsync(c => c.Name.ToUpper() == this.DetailCart.OrderHeader.CouponCode.ToUpper());

                this.DetailCart.OrderHeader.OrderTotal =
                    SD.DiscountedPrice(couponFromDb, this.DetailCart.OrderHeader.OrderTotalOriginal);
            }

            return this.View(this.DetailCart);
        }

        public IActionResult AddCoupon()
        {
            this.DetailCart.OrderHeader.CouponCode = this.DetailCart.OrderHeader.CouponCode ?? string.Empty;

            this.HttpContext.Session.SetString(SD.ssCouponCode, this.DetailCart.OrderHeader.CouponCode);

            return this.RedirectToAction(nameof(this.Index));
        }

        public IActionResult RemoveCoupon()
        {
            this.HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);

            return this.RedirectToAction(nameof(this.Index));
        }

        public async Task<IActionResult> Plus(int? cartId)
        {
            if (!cartId.HasValue)
            {
                return this.NotFound();
            }

            var cart = await this.db.ShoppingCart
                .FirstOrDefaultAsync(c => c.Id == cartId.Value);

            if (cart == null)
            {
                return this.NotFound();
            }

            cart.Count++;

            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }

        public async Task<IActionResult> Minus(int? cartId)
        {
            if (!cartId.HasValue)
            {
                return this.NotFound();
            }

            var cart = await this.db.ShoppingCart
                .FirstOrDefaultAsync(c => c.Id == cartId.Value);

            if (cart == null)
            {
                return this.NotFound();
            }

            if (cart.Count == 1)
            {
                this.db.ShoppingCart.Remove(cart);

                var count = await this.db.ShoppingCart.CountAsync(c => c.ApplicationUserId == cart.ApplicationUserId);
                this.HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);
            }
            else
            {
                cart.Count--;
            }

            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.Index));
        }

        public async Task<IActionResult> Remove(int? cartId)
        {
            if (!cartId.HasValue)
            {
                return this.NotFound();
            }

            var cart = await this.db.ShoppingCart
                .FirstOrDefaultAsync(c => c.Id == cartId.Value);

            if (cart == null)
            {
                return this.NotFound();
            }

            this.db.ShoppingCart.Remove(cart);
            await this.db.SaveChangesAsync();

            var count = await this.db.ShoppingCart.CountAsync(c => c.ApplicationUserId == cart.ApplicationUserId);
            this.HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);

            return this.RedirectToAction(nameof(this.Index));
        }

        public async Task<IActionResult> Summary()
        {
            return this.NotFound();
        }
    }
}