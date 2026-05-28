using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.WarehouseStaff)]
    public class WarehouseOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OrderService _orderService;
        private readonly UserActivityService _activity;

        public WarehouseOrderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            OrderService orderService,
            UserActivityService activity)
        {
            _context     = context;
            _userManager = userManager;
            _orderService = orderService;
            _activity = activity;
        }

        private async Task<string?> GetParentAdminIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.ParentAdminId;
        }

        // ── Index ──────────────────────────────────────────────────────────
        // Supported filter values: "Pending" (default), "Processed", or any OrderStatus string.
        // "Processed" = orders where a route has been assigned by the Main L-Staff.
        public async Task<IActionResult> Index(string? status)
        {
            var adminId = await GetParentAdminIdAsync();
            if (string.IsNullOrEmpty(adminId)) return Forbid();

            var query = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Buyer)
                .Include(o => o.RouteAssignment)
                .Where(o => o.CreatedByAdminId == adminId)
                .AsQueryable();

            var currentFilter = status ?? OrderStatus.Pending;

            if (currentFilter == "Processed")
            {
                // Orders that have been approved AND already have a route assigned
                query = query.Where(o =>
                    o.Status == OrderStatus.Approved &&
                    o.RouteAssignment != null);
            }
            else if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);

                // For the Approved tab, exclude already-processed ones
                if (status == OrderStatus.Approved)
                    query = query.Where(o => o.RouteAssignment == null);
            }
            else
            {
                query = query.Where(o => o.Status == OrderStatus.Pending);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            ViewBag.CurrentStatus = currentFilter;
            return View(orders);
        }

        // ── Detail ─────────────────────────────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var adminId = await GetParentAdminIdAsync();
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Buyer)
                .Include(o => o.StatusLogs)
                .FirstOrDefaultAsync(o => o.Id == id && o.CreatedByAdminId == adminId);

            if (order == null) return NotFound();
            return View(order);
        }

        // ── Scan ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Scan(string? orderNumber)
        {
            var adminId = await GetParentAdminIdAsync();

            if (string.IsNullOrWhiteSpace(orderNumber))
                return View();

            orderNumber = ExtractOrderNumber(orderNumber);

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Buyer)
                .Include(o => o.StatusLogs)
                .FirstOrDefaultAsync(o =>
                    o.OrderNumber == orderNumber &&
                    o.CreatedByAdminId == adminId);

            if (order == null)
            {
                return View("ScanError",
                    $"Order number '{orderNumber}' was not found.");
            }

            // ALWAYS OPEN DETAIL PAGE
            return RedirectToAction(nameof(Detail), new { id = order.Id });
        }

        private static string ExtractOrderNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim();

            // If QR contains full URL
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

                if (query.TryGetValue("orderNumber", out var values))
                {
                    var orderNo = values.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(orderNo))
                        return orderNo.Trim();
                }

                // fallback last URL segment
                var lastSegment = uri.Segments.LastOrDefault()?.Trim('/');

                if (!string.IsNullOrWhiteSpace(lastSegment))
                    return lastSegment;
            }

            // Raw QR text
            return input;
        }

        // ── Approve → notify Main L-Staff ──────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReadyForPickup(int orderId)
        {
            var adminId = await GetParentAdminIdAsync();
            var user    = await _userManager.GetUserAsync(User);

            var order = await _context.Orders
                .Include(o => o.StatusLogs)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CreatedByAdminId == adminId);

            if (order == null) return NotFound();

            order.Status    = OrderStatus.Approved;
            order.UpdatedAt = DateTime.UtcNow;

            // Buyer-visible approval log
            order.StatusLogs.Add(new OrderStatusLog
            {
                Status           = OrderStatus.Approved,
                Note             = "Warehouse confirmed and preparing your package.",
                UpdatedByUserId  = user?.Id,
                UpdatedByName    = user?.FullName,
                IsVisibleToBuyer = true,   // ← buyer sees this
                CreatedAt        = DateTime.UtcNow
            });

            // Find the Main L-Staff (AssignedHub == null, same company admin)
            var allLStaff  = await _userManager.GetUsersInRoleAsync(RoleNames.LogisticStaff);
            var mainLStaff = allLStaff.FirstOrDefault(u =>
                u.ParentAdminId == adminId && u.AssignedHub == null);

            if (mainLStaff != null)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = mainLStaff.Id,
                    Title           = "New Order Ready for Routing",
                    Message         = $"Order {order.OrderNumber} approved by warehouse. Assign route.",
                    ActionUrl       = $"/LogisticStaffOrder/Detail/{order.Id}",
                    CreatedAt       = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            await _activity.LogAsync(user!.Id, UserAction.OrderApproved,
                $"Order {order.OrderNumber} approved by warehouse staff.",
                "Order", order.OrderNumber);
                
            TempData["Success"] = $"Order {order.OrderNumber} approved. Main logistic staff notified.";
            return RedirectToAction(nameof(Index));
        }

        // ── Generic status update ───────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status, string? note)
        {
            var adminId = await GetParentAdminIdAsync();
            var user    = await _userManager.GetUserAsync(User);

            var allowed = new[] { OrderStatus.Pending, OrderStatus.Approved, OrderStatus.OutForDelivery, OrderStatus.Delivered, OrderStatus.Cancelled };
            if (!allowed.Contains(status)) return BadRequest("Invalid status.");

            var order = await _context.Orders
                .Include(o => o.StatusLogs)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CreatedByAdminId == adminId);
            if (order == null) return NotFound();

            order.Status    = status;
            order.UpdatedAt = DateTime.UtcNow;
            order.StatusLogs.Add(new OrderStatusLog
            {
                Status           = status,
                Note             = note,
                UpdatedByUserId  = user?.Id,
                UpdatedByName    = user?.FullName,
                IsVisibleToBuyer = true,
                CreatedAt        = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var logAction = status switch
            {
                OrderStatus.Approved => UserAction.OrderApproved,
                OrderStatus.Cancelled => UserAction.OrderCancelled,
                _ => status   // fallback: use the status string itself
            };

            await _activity.LogAsync(user!.Id, logAction,
                $"Order {order.OrderNumber} status updated to {status}.",
                "Order", order.OrderNumber);

            TempData["Success"] = $"Order {order.OrderNumber} updated to {status}.";
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ── Download Shipping Label ─────────────────────────────────────────
        public async Task<IActionResult> DownloadLabel(int id)
        {
            var adminId = await GetParentAdminIdAsync();
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.CreatedByAdminId == adminId);
            if (order == null) return NotFound();

            if (string.IsNullOrEmpty(order.QrCodePath))
            {
                order.QrCodePath = _orderService.GenerateQrCode(order.OrderNumber);
            }
            else
            {
                var qp = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    order.QrCodePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!System.IO.File.Exists(qp))
                    order.QrCodePath = _orderService.GenerateQrCode(order.OrderNumber);
            }

            var totalWeight = order.Items.Sum(i => (i.Product?.Weight ?? 0) * i.Quantity);
            order.ShippingLabelPath = _orderService.GenerateShippingLabelPdf(order, totalWeight);
            await _context.SaveChangesAsync();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                order.ShippingLabelPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(filePath)) return NotFound();

            return PhysicalFile(filePath, "application/pdf", $"ShippingLabel-{order.OrderNumber}.pdf");
        }
    }
}
