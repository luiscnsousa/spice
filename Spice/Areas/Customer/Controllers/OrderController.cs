namespace Spice.Areas.Customer.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Spice.Data;
    using Spice.Models;
    using Spice.Models.ViewModels;

    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext db;
        private int PageSize = 4;

        public OrderController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public IActionResult Index()
        {
            return View();
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
    }
}