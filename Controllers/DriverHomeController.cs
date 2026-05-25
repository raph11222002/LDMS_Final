using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.Driver)]
    public class DriverHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DriverHomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ─────────────────────────────────────────────────────────────
        //  INDEX – Driver Dashboard
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string filter = "Pending")
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Forbid();

            var allStops = await _context.OrderHubStops
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.Buyer)
                .Where(s => s.AssignedDriverId == driver.Id)
                .OrderByDescending(s => s.RouteAssignment.CreatedAt)
                .ToListAsync();

            var filtered = filter switch
            {
                "InTransit" => allStops.Where(s => s.StopStatus == HubStopStatus.InTransit).ToList(),
                "Completed" => allStops.Where(s => s.StopStatus == HubStopStatus.Arrived
                                                  || s.StopStatus == HubStopStatus.Completed).ToList(),
                _ => allStops.Where(s => s.StopStatus == HubStopStatus.Pending).ToList()
            };

            var model = new DriverDashboardViewModel
            {
                Driver = driver,
                AllStops = filtered,
                ActiveFilter = filter,
                TotalAssigned = allStops.Count,
                PendingCount = allStops.Count(s => s.StopStatus == HubStopStatus.Pending),
                InTransitCount = allStops.Count(s => s.StopStatus == HubStopStatus.InTransit),
                CompletedCount = allStops.Count(s => s.StopStatus == HubStopStatus.Arrived
                                                  || s.StopStatus == HubStopStatus.Completed)
            };

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────
        //  SCAN PAGE
        // ─────────────────────────────────────────────────────────────
        public IActionResult Scan() => View();

        // ─────────────────────────────────────────────────────────────
        //  AJAX: Lookup order by QR/order number
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> LookupOrder(string orderNumber)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Forbid();

            var stop = await _context.OrderHubStops
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.Buyer)
                .FirstOrDefaultAsync(s =>
                    s.AssignedDriverId == driver.Id &&
                    s.RouteAssignment.Order.OrderNumber == orderNumber &&
                    (s.StopStatus == HubStopStatus.Pending ||
                     s.StopStatus == HubStopStatus.InTransit));

            if (stop == null)
                return Json(new { found = false, message = "No active assignment found for this order." });

            return Json(new
            {
                found = true,
                stopId = stop.Id,
                orderNumber = stop.RouteAssignment.Order.OrderNumber,
                hubLabel = stop.HubLabel,
                driverLabel = stop.DriverLabel,
                status = stop.StopStatus.ToString(),
                buyerName = stop.RouteAssignment.Order.Buyer?.FullName,
                address = stop.RouteAssignment.ResolvedAddress
                              ?? stop.RouteAssignment.Order.DeliveryAddress
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  POST: Update stop status
        // ─────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int stopId, string action)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Forbid();

            var stop = await _context.OrderHubStops
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.StatusLogs)
                .FirstOrDefaultAsync(s =>
                    s.Id == stopId && s.AssignedDriverId == driver.Id);

            if (stop == null) return NotFound();

            var order = stop.RouteAssignment.Order;

            if (action == "start" && stop.StopStatus == HubStopStatus.Pending)
            {
                stop.StopStatus = HubStopStatus.InTransit;

                // Buyer-visible log
                order.StatusLogs.Add(new OrderStatusLog
                {
                    Status = "In Transit",
                    Note = $"Your parcel is on its way to {stop.HubLabel}.",
                    UpdatedByUserId = driver.Id,
                    UpdatedByName = driver.FullName,
                    IsVisibleToBuyer = true,
                    CreatedAt = DateTime.UtcNow
                });

                TempData["Success"] = $"Trip started for {order.OrderNumber}.";
            }
            else if (action == "arrived" && stop.StopStatus == HubStopStatus.InTransit)
            {
                stop.StopStatus = HubStopStatus.Arrived;

                // Buyer-visible log
                order.StatusLogs.Add(new OrderStatusLog
                {
                    Status = "Arrived at Hub",
                    Note = $"Your parcel has arrived at {stop.HubLabel}.",
                    UpdatedByUserId = driver.Id,
                    UpdatedByName = driver.FullName,
                    IsVisibleToBuyer = true,
                    CreatedAt = DateTime.UtcNow
                });

                // Internal log
                order.StatusLogs.Add(new OrderStatusLog
                {
                    Status = "Hub Arrived",
                    Note = $"Driver {driver.FullName} arrived at {stop.HubLabel}. Awaiting next leg assignment.",
                    UpdatedByUserId = driver.Id,
                    UpdatedByName = driver.FullName,
                    IsVisibleToBuyer = false,
                    CreatedAt = DateTime.UtcNow
                });

                // Notify ALL L-Staff assigned to this hub
                await NotifyHubStaffOnArrivalAsync(stop, order);

                TempData["Success"] = $"Marked arrived at {stop.HubLabel}. Hub staff has been notified.";
            }

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────────────────────
        //  Helper: Notify hub L-Staff when driver arrives
        // ─────────────────────────────────────────────────────────────
        private async Task NotifyHubStaffOnArrivalAsync(OrderHubStop stop, Order order)
        {
            // Find the hub enum value from the stop
            var hub = stop.Hub;

            var allLStaff = await _userManager.GetUsersInRoleAsync(RoleNames.LogisticStaff);
            var hubStaff = allLStaff.Where(u =>
                u.ParentAdminId == order.CreatedByAdminId &&
                u.AssignedHub == hub);

            foreach (var ls in hubStaff)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = ls.Id,
                    Title = $"Parcel Arrived – {DeliveryHubInfo.Hubs[hub].ShortName}",
                    Message = $"Order {order.OrderNumber} has arrived at your hub. Please assign the next driver.",
                    ActionUrl = $"/LogisticStaffOrder/Detail/{order.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Order Detail for driver
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> OrderDetail(int id)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Forbid();

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Buyer)
                .Include(o => o.StatusLogs.Where(l => l.IsVisibleToBuyer))
                .Include(o => o.RouteAssignment)
                    .ThenInclude(r => r!.HubStops)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Verify this driver is assigned to at least one stop on this order
            var myStop = order.RouteAssignment?.HubStops
                .FirstOrDefault(s => s.AssignedDriverId == driver.Id);

            if (myStop == null) return Forbid();

            ViewBag.MyStop = myStop;
            return View(order);
        }
    }
}