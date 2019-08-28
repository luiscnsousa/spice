namespace Spice.Areas.Customer.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using Spice.Models.ViewModels;
    using Spice.Utility;

    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IEmailSender emailSender;
        private int PageSize = 3;

        public OrderController(ApplicationDbContext db, IEmailSender emailSender)
        {
            this.db = db;
            this.emailSender = emailSender;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var orderDetailsViewModel = new OrderDetailsViewModel
            {
                OrderHeader = await this.db.OrderHeader
                    .Include(h => h.ApplicationUser)
                    .FirstOrDefaultAsync(h => h.Id == id && h.UserId == claim.Value),
                OrderDetails = await this.db.OrderDetails
                    .Where(d => d.OrderId == id)
                    .ToListAsync()
            };

            return this.View(orderDetailsViewModel);
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var orderList = new List<OrderDetailsViewModel>();

            var orderHeaderList = await this.db.OrderHeader
                .Include(h => h.ApplicationUser)
                .Where(h => h.UserId == claim.Value)
                .ToListAsync();

            foreach (var item in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await this.db.OrderDetails
                        .Where(d => d.OrderId == item.Id)
                        .ToListAsync()
                };

                orderList.Add(individual);
            }

            var orderListVM = new OrderListViewModel
            {
                Orders = orderList
                    .OrderByDescending(p => p.OrderHeader.Id)
                    .Skip((productPage - 1) * this.PageSize)
                    .Take(this.PageSize)
                    .ToList(),
                PagingInfo = new PagingInfo
                {
                    CurrentPage = productPage,
                    ItemsPerPage = this.PageSize,
                    TotalItems = orderList.Count,
                    UrlParam = "/Customer/Order/OrderHistory?productPage=:"
                }
            };

            return this.View(orderListVM);
        }

        [Authorize]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var orderDetailViewModel = new OrderDetailsViewModel
            {
                OrderHeader = await this.db.OrderHeader.FirstOrDefaultAsync(h => h.Id == id),
                OrderDetails = await this.db.OrderDetails.Where(d => d.OrderId == id).ToListAsync()
            };

            orderDetailViewModel.OrderHeader.ApplicationUser =
                await this.db.ApplicationUser.FirstOrDefaultAsync(u => u.Id == orderDetailViewModel.OrderHeader.UserId);

            return this.PartialView("_IndividualOrderDetails", orderDetailViewModel);
        }

        [Authorize]
        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var orderHeader = await this.db.OrderHeader.FirstOrDefaultAsync(h => h.Id == id);

            return this.PartialView("_OrderStatus", orderHeader.Status);
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            var orderDetailsVM = new List<OrderDetailsViewModel>();

            var orderHeaderList = await this.db.OrderHeader
                .Where(h => h.Status == SD.StatusSubmitted || h.Status == SD.StatusInProcess)
                .OrderByDescending(h => h.PickupTime)
                .ToListAsync();

            foreach (var item in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await this.db.OrderDetails
                        .Where(d => d.OrderId == item.Id)
                        .ToListAsync()
                };

                orderDetailsVM.Add(individual);
            }

            return this.View(orderDetailsVM.OrderBy(d => d.OrderHeader.PickupTime).ToList());
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int orderId)
        {
            var orderHeader = await this.db.OrderHeader.FindAsync(orderId);

            if (orderHeader == null)
            {
                return this.NotFound();
            }

            orderHeader.Status = SD.StatusInProcess;

            await this.db.SaveChangesAsync();

            return this.RedirectToAction(nameof(this.ManageOrder), "Order");
        }


        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderReady(int orderId)
        {
            var orderHeader = await this.db.OrderHeader.FindAsync(orderId);

            if (orderHeader == null)
            {
                return this.NotFound();
            }

            orderHeader.Status = SD.StatusReady;

            await this.db.SaveChangesAsync();

            await this.emailSender.SendEmailAsync(
                (await this.db.Users.FirstOrDefaultAsync(u => u.Id == orderHeader.UserId)).Email,
                $"Spice - Order Ready For Pickup {orderHeader.Id.ToString()}",
                "Order is ready for pickup!");

            return this.RedirectToAction(nameof(this.ManageOrder), "Order");
        }


        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int orderId)
        {
            var orderHeader = await this.db.OrderHeader.FindAsync(orderId);

            if (orderHeader == null)
            {
                return this.NotFound();
            }

            orderHeader.Status = SD.StatusCancelled;

            await this.db.SaveChangesAsync();

            await this.emailSender.SendEmailAsync(
                (await this.db.Users.FirstOrDefaultAsync(u => u.Id == orderHeader.UserId)).Email,
                $"Spice - Order Canceled {orderHeader.Id.ToString()}",
                "Order has been canceled successfully");

            return this.RedirectToAction(nameof(this.ManageOrder), "Order");
        }

        [Authorize]
        public async Task<IActionResult> OrderPickup(
            int productPage = 1,
            string searchName = null,
            string searchPhone = null,
            string searchEmail = null)
        {
            var param = new StringBuilder();
            param.Append("/Customer/Order/OrderPickup?productPage=:");

            var orderHeaderQueryable = this.db.OrderHeader
                .Include(h => h.ApplicationUser)
                .Where(h => h.Status == SD.StatusReady);

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                param.Append($"&{nameof(searchName)}={searchName}");

                orderHeaderQueryable =
                    orderHeaderQueryable.Where(h => h.PickupName.ToLower().Contains(searchName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(searchPhone))
            {
                param.Append($"&{nameof(searchPhone)}={searchPhone}");

                orderHeaderQueryable =
                    orderHeaderQueryable.Where(h => h.PhoneNumber.ToLower().Contains(searchPhone.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                param.Append($"&{nameof(searchEmail)}={searchEmail}");

                var user = await this.db.ApplicationUser.FirstOrDefaultAsync(u =>
                    u.Email.ToLower().Contains(searchEmail.ToLower()));

                orderHeaderQueryable =
                    orderHeaderQueryable.Where(h => h.UserId == user.Id);
            }

            var orderHeaderList = await orderHeaderQueryable
                .ToListAsync();

            var orderList = new List<OrderDetailsViewModel>();

            foreach (var item in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await this.db.OrderDetails
                        .Where(d => d.OrderId == item.Id)
                        .ToListAsync()
                };

                orderList.Add(individual);
            }

            var orderListVM = new OrderListViewModel
            {
                Orders = orderList
                    .OrderByDescending(p => p.OrderHeader.Id)
                    .Skip((productPage - 1) * this.PageSize)
                    .Take(this.PageSize)
                    .ToList(),
                PagingInfo = new PagingInfo
                {
                    CurrentPage = productPage,
                    ItemsPerPage = this.PageSize,
                    TotalItems = orderList.Count,
                    UrlParam = param.ToString()
                }
            };

            return this.View(orderListVM);
        }

        [HttpPost]
        [ActionName("OrderPickup")]
        [Authorize(Roles = SD.FrontDeskUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPickupPOST(int orderId)
        {
            var orderHeader = await this.db.OrderHeader.FindAsync(orderId);

            if (orderHeader == null)
            {
                return this.NotFound();
            }

            orderHeader.Status = SD.StatusCompleted;

            await this.db.SaveChangesAsync();

            await this.emailSender.SendEmailAsync(
                (await this.db.Users.FirstOrDefaultAsync(u => u.Id == orderHeader.UserId)).Email,
                $"Spice - Order Complete {orderHeader.Id.ToString()}",
                "Order has been completed successfully!");

            return this.RedirectToAction("OrderPickup", "Order");
        }
    }
}
