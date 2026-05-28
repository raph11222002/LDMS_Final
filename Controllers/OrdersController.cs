using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LDMS_Final.Services;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.Buyer)]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserActivityService _activity;

        public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
         UserActivityService activity)
        {
            _context = context;
            _userManager = userManager;
            _activity = activity;
        }

        private string GetBuyerId() => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index(string? status)
        {
            var buyerId = GetBuyerId();

            var query = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.BuyerId == buyerId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var model = orders.Select(o => new BuyerOrderListViewModel
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod,
                ItemCount = o.Items.Sum(i => i.Quantity),
                FirstProductImage = o.Items.FirstOrDefault()?.Product?.ImagePath1,
                CreatedAt = o.CreatedAt
            }).ToList();

            ViewBag.CurrentStatus = status;
            return View(model);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var buyerId = GetBuyerId();

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.BuyerId == buyerId);

            if (order == null) return NotFound();

            var statusHistory = await _context.OrderStatusLogs
                .Where(l => l.OrderId == order.Id && l.IsVisibleToBuyer)
                .OrderBy(l => l.CreatedAt)
                .Select(l => new OrderStatusHistoryViewModel
                {
                    Status = l.Status,
                    Note = l.Note,
                    UpdatedByName = l.UpdatedByName,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            var model = new BuyerOrderDetailViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                TotalAmount = order.TotalAmount,
                ShippingFee = order.ShippingFee,
                DeliveryFullName = order.DeliveryFullName,
                DeliveryContactNumber = order.DeliveryContactNumber,
                DeliveryAddress = order.DeliveryAddress,
                DeliveryNote = order.DeliveryNote,
                DeliveryLatitude = order.DeliveryLatitude,
                DeliveryLongitude = order.DeliveryLongitude,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new CartItemViewModel
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    VariantName = i.VariantName,
                    SelectedSize = i.SelectedSize,
                    ImagePath = i.Product?.ImagePath1,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                }).ToList(),
                StatusHistory = statusHistory
            };

            return View(model);
        }
    }
}