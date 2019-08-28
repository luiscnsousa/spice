namespace Spice.Areas.Customer.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using Spice.Models.ViewModels;
    using Spice.Utility;
    using Stripe;

    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IEmailSender emailSender;

        public CartController(ApplicationDbContext db, IEmailSender emailSender)
        {
            this.db = db;
            this.emailSender = emailSender;
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
            this.DetailCart = new OrderDetailsCart
            {
                OrderHeader = new OrderHeader()
            };

            this.DetailCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var applicationUser = await this.db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == claim.Value);

            this.DetailCart.ListCart = await this.db.ShoppingCart
                                           .Where(c => c.ApplicationUserId == claim.Value)
                                           .ToListAsync() ?? new List<ShoppingCart>();

            foreach (var list in this.DetailCart.ListCart)
            {
                list.MenuItem = await this.db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                this.DetailCart.OrderHeader.OrderTotal += list.MenuItem.Price * list.Count;
            }

            this.DetailCart.OrderHeader.OrderTotalOriginal = this.DetailCart.OrderHeader.OrderTotal;
            this.DetailCart.OrderHeader.PickupName = applicationUser?.Name;
            this.DetailCart.OrderHeader.PhoneNumber = applicationUser?.PhoneNumber;
            this.DetailCart.OrderHeader.PickupTime = DateTime.UtcNow;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPOST(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            this.DetailCart.ListCart = await this.db.ShoppingCart
                .Where(c => c.ApplicationUserId == claim.Value)
                .ToListAsync();

            this.DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            this.DetailCart.OrderHeader.OrderDate = DateTime.Now;
            this.DetailCart.OrderHeader.UserId = claim.Value;
            this.DetailCart.OrderHeader.Status = SD.PaymentStatusPending;
            this.DetailCart.OrderHeader.PickupTime = Convert.ToDateTime(
                this.DetailCart.OrderHeader.PickupDate.ToShortDateString() + " " + this.DetailCart.OrderHeader.PickupTime.ToShortTimeString());

            this.db.OrderHeader.Add(this.DetailCart.OrderHeader);
            await this.db.SaveChangesAsync();

            this.DetailCart.OrderHeader.OrderTotalOriginal = 0;

            foreach (var item in this.DetailCart.ListCart)
            {
                item.MenuItem = await this.db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                var orderDetails = new OrderDetails
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = this.DetailCart.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };

                this.DetailCart.OrderHeader.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;

                this.db.OrderDetails.Add(orderDetails);
            }

            var couponCode = this.HttpContext.Session.GetString(SD.ssCouponCode);
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                this.DetailCart.OrderHeader.CouponCode = couponCode;

                var couponFromDb = await this.db.Coupon
                    .FirstOrDefaultAsync(c => c.Name.ToUpper() == this.DetailCart.OrderHeader.CouponCode.ToUpper());

                this.DetailCart.OrderHeader.OrderTotal =
                    SD.DiscountedPrice(couponFromDb, this.DetailCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                this.DetailCart.OrderHeader.OrderTotal = this.DetailCart.OrderHeader.OrderTotalOriginal;
            }

            this.DetailCart.OrderHeader.CouponCodeDiscount =
                this.DetailCart.OrderHeader.OrderTotalOriginal - this.DetailCart.OrderHeader.OrderTotal;

            this.db.ShoppingCart.RemoveRange(this.DetailCart.ListCart);
            this.HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);
            await this.db.SaveChangesAsync();

            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(this.DetailCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID : " + this.DetailCart.OrderHeader.Id,
                SourceId = stripeToken
            };

            var service = new ChargeService();
            var charge = await service.CreateAsync(options);

            if (charge.BalanceTransactionId == null)
            {
                this.DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            else
            {
                this.DetailCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower() == "succeeded")
            {
                await this.emailSender.SendEmailAsync(
                    (await this.db.Users.FirstOrDefaultAsync(u => u.Id == claim.Value)).Email,
                    $"Spice - Order created {this.DetailCart.OrderHeader.Id.ToString()}",
                    "Order has been submitted successfully!");

                this.DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                this.DetailCart.OrderHeader.Status = SD.StatusSubmitted;
            }
            else
            {
                this.DetailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }

            await this.db.SaveChangesAsync();

            //return this.RedirectToAction("Index", "Home");
            return this.RedirectToAction("Confirm", "Order", new { id = this.DetailCart.OrderHeader.Id });
        }
    }
}