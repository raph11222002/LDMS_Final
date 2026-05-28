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
    [Authorize(Roles = RoleNames.LogisticStaff)]
    public class LogisticStaffOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly UserActivityService _activity;

        public LogisticStaffOrderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            UserActivityService activity)
        {
            _context     = context;
            _userManager = userManager;
            _config      = config;
            _activity = activity;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        private async Task<ApplicationUser?> GetCurrentStaffAsync()
            => await _userManager.GetUserAsync(User);

        private static bool IsMain(ApplicationUser staff) => staff.AssignedHub == null;

        /// <summary>
        /// Notify every L-Staff assigned to a hub that a parcel is incoming.
        /// </summary>
        private async Task NotifyHubStaffAsync(
            DeliveryHub hub, string companyAdminId, string orderNumber, int orderId)
        {
            var allLStaff = await _userManager.GetUsersInRoleAsync(RoleNames.LogisticStaff);
            var hubStaff  = allLStaff.Where(u =>
                u.ParentAdminId == companyAdminId &&
                u.AssignedHub   == hub);

            foreach (var ls in hubStaff)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = ls.Id,
                    Title           = $"Incoming Parcel – {DeliveryHubInfo.Hubs[hub].ShortName} Hub",
                    Message         = $"Order {orderNumber} is on its way to your sorting center.",
                    ActionUrl       = $"/LogisticStaffOrder/Detail/{orderId}",
                    CreatedAt       = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Notify Driver 1 (warehouse driver assigned to leg 0) that route is confirmed
        /// and they should pick up the parcel.
        /// </summary>
        private void NotifyDriver(string driverId, string orderNumber, int orderId)
        {
            _context.Notifications.Add(new Notification
            {
                RecipientUserId = driverId,
                Title           = "Pickup Assignment",
                Message         = $"You have been assigned to pick up Order {orderNumber} from the warehouse.",
                ActionUrl       = $"/DriverHome/OrderDetail/{orderId}",
                CreatedAt       = DateTime.UtcNow
            });
        }

        // ─────────────────────────────────────────────────────────────────
        //  INDEX – order list
        // ─────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string? status)
        {
            var staff = await GetCurrentStaffAsync();
            if (staff == null) return Forbid();
            var companyAdminId = staff.ParentAdminId!;

            IQueryable<Order> query;

            if (IsMain(staff))
            {
                // Main L-Staff sees all approved+ orders for the company
                query = _context.Orders
                    .Include(o => o.Items).ThenInclude(i => i.Product)
                    .Include(o => o.Buyer)
                    .Include(o => o.RouteAssignment)
                    .Where(o => o.CreatedByAdminId == companyAdminId
                             && o.Status != OrderStatus.Pending);
            }
            else
            {
                // Hub L-Staff sees only orders whose route passes through their hub
                query = _context.Orders
                    .Include(o => o.Items).ThenInclude(i => i.Product)
                    .Include(o => o.Buyer)
                    .Include(o => o.StatusLogs)
                    .Include(o => o.RouteAssignment).ThenInclude(r => r!.HubStops)
                    .Where(o => o.CreatedByAdminId == companyAdminId
                             && o.RouteAssignment != null
                             && o.RouteAssignment.HubStops
                                    .Any(s => (int)s.Hub == (int)staff.AssignedHub!.Value));
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            var orders = await query
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();

            // Mark all notifications as read when visiting the list
            await _context.Notifications
                .Where(n => n.RecipientUserId == staff.Id && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            var model = new LogisticOrderListViewModel
            {
                Orders        = orders,
                CurrentStatus = status ?? string.Empty
            };

            ViewBag.IsMainLStaff = IsMain(staff);
            ViewBag.CurrentStaffId = staff.Id;
            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────
        //  DETAIL
        // ─────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var staff = await GetCurrentStaffAsync();
            if (staff == null) return Forbid();
            var companyAdminId = staff.ParentAdminId!;

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Buyer)
                .Include(o => o.StatusLogs)
                .Include(o => o.RouteAssignment)
                    .ThenInclude(r => r!.HubStops)
                        .ThenInclude(s => s.AssignedDriver)
                .Include(o => o.RouteAssignment)
                    .ThenInclude(r => r!.HubStops)
                        .ThenInclude(s => s.OutgoingDriver)
                .FirstOrDefaultAsync(o => o.Id == id && o.CreatedByAdminId == companyAdminId);

            if (order == null) return NotFound();

            // ── Drivers this L-Staff can assign ──────────────────────────
            var allDrivers = await _userManager.GetUsersInRoleAsync(RoleNames.Driver);
            var myDriverUsers = allDrivers
                .Where(d => d.ParentAdminId == companyAdminId
                         && d.IsActive
                         && d.AssignedHub == staff.AssignedHub)
                .ToList();

            var driverIds = myDriverUsers.Select(d => d.Id).ToList();
            var vehicles = await _context.DriverVehicles
                .Where(v => driverIds.Contains(v.DriverId) && v.IsActive)
                .ToListAsync();

            var allMapped = myDriverUsers.Select(d =>
            {
                var vehicle = vehicles.FirstOrDefault(v => v.DriverId == d.Id);
                return new DriverWithVehicleViewModel
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    VehicleType = vehicle?.VehicleType ?? string.Empty,
                    PlateNumber = vehicle?.PlateNumber ?? string.Empty
                };
            }).ToList();

            var vanDrivers = allMapped
                .Where(d => !d.VehicleType.Contains("Motorcycle", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var motorcycleDrivers = allMapped
                .Where(d => d.VehicleType.Contains("Motorcycle", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // ── Suggested hubs ────────────────────────────────────────────
            double? destLat = order.DeliveryLatitude
                ?? (double?)(order.RouteAssignment?.ResolvedLatitude);

            var suggested = destLat.HasValue
                ? DeliveryHubInfo.DetermineRequiredHubs(destLat.Value)
                : new List<DeliveryHub>();

            var model = new LogisticOrderDetailViewModel
            {
                Order = order,
                RouteAssignment = order.RouteAssignment,
                AvailableDrivers = vanDrivers,
                MotorcycleDrivers = motorcycleDrivers,
                SuggestedHubs = suggested,
                MapboxToken = _config["Mapbox:AccessToken"] ?? string.Empty,
                IsMainLStaff = IsMain(staff),
                CurrentStaffHub = staff.AssignedHub
            };

            ViewBag.IsMainLStaff = IsMain(staff);
            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────
        //  POST: ASSIGN ROUTE (Main L-Staff only)
        // ─────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRoute(AssignRouteViewModel vm)
        {
            var staff = await GetCurrentStaffAsync();
            if (staff == null || !IsMain(staff)) return Forbid();
            var companyAdminId = staff.ParentAdminId!;

            var order = await _context.Orders
                .Include(o => o.StatusLogs)
                .Include(o => o.RouteAssignment).ThenInclude(r => r!.HubStops)
                .FirstOrDefaultAsync(o => o.Id == vm.OrderId && o.CreatedByAdminId == companyAdminId);

            if (order == null) return NotFound();

            // Remove old route if re-assigning
            if (order.RouteAssignment != null)
            {
                _context.OrderHubStops.RemoveRange(order.RouteAssignment.HubStops);
                _context.OrderRouteAssignments.Remove(order.RouteAssignment);
            }

            // Parse hubs
            var hubValues = (vm.SelectedHubs ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(h => int.TryParse(h.Trim(), out var v) ? (int?)v : null)
                .Where(v => v.HasValue)
                .Select(v => (DeliveryHub)v!.Value)
                .ToList();

            var legDict = vm.LegAssignments.ToDictionary(l => l.LegIndex);
            var stops = new List<OrderHubStop>();

            for (int i = 0; i < hubValues.Count; i++)
            {
                var hub = hubValues[i];
                var hubInfo = DeliveryHubInfo.Hubs[hub];
                var leg = legDict.GetValueOrDefault(i);
                var drvNum = i + 1;

                stops.Add(new OrderHubStop
                {
                    StopOrder = i,
                    Hub = hub,
                    HubLabel = hubInfo.Label,
                    AssignedDriverId = i == 0 ? leg?.AssignedDriverId : null,
                    DriverLabel = $"van driver {drvNum}",
                    StopStatus = HubStopStatus.Pending
                });
            }

            // ── NO extra stops.Add here — last hub stop IS the final stop ──
            // ── Motorcycle driver for door-to-door is assigned later by    ──
            // ── the last hub's L-Staff via AssignNextDriver                ──

            var route = new OrderRouteAssignment
            {
                OrderId = order.Id,
                AssignedByStaffId = staff.Id,
                ResolvedLatitude = vm.ResolvedLatitude.HasValue ? (decimal)vm.ResolvedLatitude.Value : null,
                ResolvedLongitude = vm.ResolvedLongitude.HasValue ? (decimal)vm.ResolvedLongitude.Value : null,
                ResolvedAddress = vm.ResolvedAddress,
                HubStops = stops,
                CreatedAt = DateTime.UtcNow
            };
            _context.OrderRouteAssignments.Add(route);

            order.StatusLogs.Add(new OrderStatusLog
            {
                Status = "Order processed",
                Note = IsMain(staff)
                    ? $"Route assigned by {staff.FullName}. (Warehouse Origin)"
                    : $"Route assigned by {staff.FullName}. " +
                    $"Hubs: {string.Join(" → ", hubValues.Select(h => DeliveryHubInfo.Hubs[h].ShortName))}.",
                UpdatedByUserId = staff.Id,
                UpdatedByName = staff.FullName,
                IsVisibleToBuyer = false,
                CreatedAt = DateTime.UtcNow
            });
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var firstStop = stops.FirstOrDefault(s => s.StopOrder == 0);
            if (firstStop?.AssignedDriverId != null)
                NotifyDriver(firstStop.AssignedDriverId, order.OrderNumber, order.Id);

            foreach (var hub in hubValues)
                await NotifyHubStaffAsync(hub, companyAdminId, order.OrderNumber, order.Id);

            await _context.SaveChangesAsync();

            TempData["RouteSuccess"] = "true";
            TempData["RouteOrderNum"] = order.OrderNumber;
            TempData["RouteHubs"] = string.Join(" → ", hubValues.Select(h => DeliveryHubInfo.Hubs[h].ShortName));
            TempData["RouteDriver"] = firstStop?.AssignedDriverId != null
                ? (await _userManager.FindByIdAsync(firstStop.AssignedDriverId))?.FullName
                : "—";

            await _activity.LogAsync(User, UserAction.RouteAssigned,       // ← ADD
                $"Route assigned for Order {order.OrderNumber}. Hubs: {TempData["RouteHubs"]}.",
                "Order", order.OrderNumber);

            return RedirectToAction(nameof(Detail), new { id = order.Id });
        }

        // ─────────────────────────────────────────────────────────────────
        //  POST: Hub L-Staff assigns driver for next leg
        // ─────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignNextDriver(AssignNextDriverViewModel vm)
        {
            var staff = await GetCurrentStaffAsync();
            if (staff == null || IsMain(staff)) return Forbid();
            var companyAdminId = staff.ParentAdminId!;

            var stop = await _context.OrderHubStops
                .Include(s => s.RouteAssignment).ThenInclude(r => r.Order)
                    .ThenInclude(o => o.StatusLogs)
                .FirstOrDefaultAsync(s => s.Id == vm.HubStopId
                    && s.RouteAssignment.Order.CreatedByAdminId == companyAdminId
                    && s.StopStatus == HubStopStatus.Arrived);

            if (stop == null) return NotFound();

            var driver = await _userManager.FindByIdAsync(vm.AssignedDriverId);
            if (driver == null || driver.AssignedHub != staff.AssignedHub)
                return BadRequest("Invalid driver.");

            // ── Determine if this is the final delivery leg ───────────────
            var nextStop = await _context.OrderHubStops
                .FirstOrDefaultAsync(s =>
                    s.OrderRouteAssignmentId == stop.OrderRouteAssignmentId &&
                    s.StopOrder == stop.StopOrder + 1);

            bool isFinalLeg = nextStop == null;

            // ── Validate motorcycle driver for final leg ──────────────────
            /*if (isFinalLeg)
            {
                var driverVehicle = await _context.DriverVehicles
                    .FirstOrDefaultAsync(v => v.DriverId == vm.AssignedDriverId && v.IsActive);

                if (driverVehicle == null ||
                    !driverVehicle.VehicleType.Contains("Motorcycle", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Final door-to-door delivery must be assigned to a motorcycle driver.";
                    return RedirectToAction(nameof(Detail), new { id = vm.OrderId });
                }
            }*/

            if (isFinalLeg)
            {
                var motoStop = new OrderHubStop
                {
                    OrderRouteAssignmentId = stop.OrderRouteAssignmentId,
                    StopOrder = stop.StopOrder + 1,
                    Hub = stop.Hub,
                    HubLabel = "Door-to-door delivery",
                    AssignedDriverId = vm.AssignedDriverId,
                    DriverLabel = "motorcycle driver",
                    StopStatus = HubStopStatus.Pending
                };
                _context.OrderHubStops.Add(motoStop);

                // ✅ Do NOT complete the hub stop here.
                // It should stay as Arrived until the moto driver scans start.
            }

            var order = stop.RouteAssignment.Order;

            // ── Mark current stop as Completed ───────────────────────────
            //stop.StopStatus = HubStopStatus.Completed;

            // ── Notify old driver they were removed ──────────────────────────
            if (isFinalLeg && stop.OutgoingDriverId != null && stop.OutgoingDriverId != vm.AssignedDriverId)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = stop.OutgoingDriverId,
                    Title = "Assignment Removed",
                    Message = $"You have been unassigned from Order {order.OrderNumber}.",
                    ActionUrl = $"/DriverHome/OrderDetail/{order.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (!isFinalLeg && nextStop?.AssignedDriverId != null && nextStop.AssignedDriverId != vm.AssignedDriverId)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = nextStop.AssignedDriverId,
                    Title = "Assignment Removed",
                    Message = $"You have been unassigned from Order {order.OrderNumber}.",
                    ActionUrl = $"/DriverHome/OrderDetail/{order.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (nextStop != null)
            {
                // Normal leg — assign van driver to next hub stop
                nextStop.AssignedDriverId = vm.AssignedDriverId;
                nextStop.DriverLabel = $"van driver {nextStop.StopOrder + 1}";
            }
            /* Remove this entire else block — it's the old approach, conflicts with new motoStop
            else
            {
                // Final leg — store motorcycle driver on OutgoingDriverId
                stop.OutgoingDriverId = vm.AssignedDriverId;
                stop.OutgoingDriverLabel = "motorcycle driver";
            } */

            // ── Status log (internal) ─────────────────────────────────────
            order.StatusLogs.Add(new OrderStatusLog
            {
                OrderId = order.Id,
                Status = "Order processed",
                Note = isFinalLeg
                    ? $"Motorcycle driver {driver.FullName} assigned for door-to-door delivery " +
                      $"by {staff.FullName} ({DeliveryHubInfo.Hubs[stop.Hub].ShortName} hub)."
                    : $"Next leg driver assigned by {staff.FullName} " +
                      $"({DeliveryHubInfo.Hubs[stop.Hub].ShortName} hub). " +
                      $"Driver: {driver.FullName}.",
                UpdatedByUserId = staff.Id,
                UpdatedByName = staff.FullName,
                IsVisibleToBuyer = false,
                CreatedAt = DateTime.UtcNow
            });

            // ── Notify assigned driver ────────────────────────────────────
            NotifyDriver(vm.AssignedDriverId, order.OrderNumber, order.Id);

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activity.LogAsync(User, UserAction.DriverAssigned,      // ← ADD
                $"Driver {driver.FullName} assigned for Order {order.OrderNumber} " +
                $"{(isFinalLeg ? "(door-to-door)" : "next leg")}.",
                "Order", order.OrderNumber);

            TempData["Success"] = $"Driver {driver.FullName} assigned for next leg.";
            return RedirectToAction(nameof(Detail), new { id = order.Id });
        }

        // ─────────────────────────────────────────────────────────────────
        //  AJAX: Unread notification count
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var staff = await GetCurrentStaffAsync();
            if (staff == null) return Json(new { count = 0 });
            var count = await _context.Notifications
                .CountAsync(n => n.RecipientUserId == staff.Id && !n.IsRead);
            return Json(new { count });
        }

        // ─────────────────────────────────────────────────────────────────
        //  AJAX: Notification list
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var staff = await GetCurrentStaffAsync();
            if (staff == null) return Json(Array.Empty<object>());
            var notifs = await _context.Notifications
                .Where(n => n.RecipientUserId == staff.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .Select(n => new { n.Id, n.Title, n.Message, n.ActionUrl, n.IsRead, n.CreatedAt })
                .ToListAsync();
            return Json(notifs);
        }

        // ─────────────────────────────────────────────────────────────────
        //  POST: Mark one notification read
        // ─────────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var staff = await GetCurrentStaffAsync();
            if (staff == null) return Forbid();
            await _context.Notifications
                .Where(n => n.Id == id && n.RecipientUserId == staff.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return Ok();
        }
    }
}
