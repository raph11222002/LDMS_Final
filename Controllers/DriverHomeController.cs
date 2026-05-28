using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using LDMS_Final.Services;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.Driver)]
    public class DriverHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly UserActivityService _activity;

        public DriverHomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            UserActivityService activity)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
            _activity = activity;
        }

        // ─────────────────────────────────────────────────────────────
        //  INDEX – Driver Dashboard
        // ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index(string filter = "Pending")
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Forbid();

            // Fetch stops where this driver is either the leg driver OR the outgoing (final-leg) driver
            /*var allStops = await _context.OrderHubStops
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.Buyer)
                .Where(s => s.AssignedDriverId == driver.Id
                         || s.OutgoingDriverId == driver.Id)   // ← ADD THIS
                .OrderByDescending(s => s.RouteAssignment.CreatedAt)
                .ToListAsync();*/

            var allStops = await _context.OrderHubStops
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.Buyer)
                .Where(s => s.AssignedDriverId == driver.Id)  // ✅ clean, no OutgoingDriverId
                .OrderByDescending(s => s.RouteAssignment.CreatedAt)
                .ToListAsync();

            var filtered = filter switch
            {
                "InTransit" => allStops.Where(s => s.StopStatus == HubStopStatus.InTransit).ToList(),

                "Completed" => allStops.Where(s =>
                    // Van driver's own stop is Arrived or Completed
                    (s.AssignedDriverId == driver.Id &&
                        (s.StopStatus == HubStopStatus.Arrived || s.StopStatus == HubStopStatus.Completed))
                    ||
                    // Motorcycle driver's stop — only count as completed when truly Completed
                    (s.OutgoingDriverId == driver.Id &&
                        s.StopStatus == HubStopStatus.Completed)
                ).ToList(),

                // Pending: motorcycle driver sees the stop as Pending until they scan start
                _ => allStops.Where(s => s.StopStatus == HubStopStatus.Pending).ToList()
            };

            var model = new DriverDashboardViewModel
            {
                Driver = driver,
                CurrentDriverId = driver.Id,
                AllStops = filtered,
                ActiveFilter = filter,
                TotalAssigned = allStops.Count,
                PendingCount = allStops.Count(s => s.StopStatus == HubStopStatus.Pending),
                InTransitCount = allStops.Count(s => s.StopStatus == HubStopStatus.InTransit),

                // ✅ Same split logic for counts
                CompletedCount = allStops.Count(s =>
                    (s.AssignedDriverId == driver.Id &&
                        (s.StopStatus == HubStopStatus.Arrived || s.StopStatus == HubStopStatus.Completed))
                    ||
                    (s.OutgoingDriverId == driver.Id &&
                        s.StopStatus == HubStopStatus.Completed))
            };

            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────
        //  DRIVER LOGS – full activity history for the logged-in driver
        // ─────────────────────────────────────────────────────────────────
        // Add this action inside DriverHomeController
        // Also add the DriverLogsViewModel class at the bottom of this file.

        public async Task<IActionResult> Logs(string? tab)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Forbid();

            tab ??= "deliveries";

            // ── 1. All hub stops assigned to this driver ──────────────────
            var allStops = await _context.OrderHubStops
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.Items)
                            .ThenInclude(i => i.Product)
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.Buyer)
                .Where(s => s.AssignedDriverId == driver.Id)
                .OrderByDescending(s => s.RouteAssignment.CreatedAt)
                .ToListAsync();

            // ── 2. Delivery reports submitted by this driver ──────────────
            var deliveryReports = await _context.RiderDeliveredReports
                .Include(r => r.Order)
                    .ThenInclude(o => o.Buyer)
                .Include(r => r.HubStop)
                .Where(r => r.DriverId == driver.Id)
                .OrderByDescending(r => r.DeliveredAt)
                .ToListAsync();

            // ── 3. Order status logs updated by this driver ───────────────
            var statusLogs = await _context.OrderStatusLogs
                .Include(l => l.Order)
                .Where(l => l.UpdatedByUserId == driver.Id)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            // ── 4. Notifications received by this driver ──────────────────
            var notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == driver.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            // ── 5. Vehicle info ───────────────────────────────────────────
            var vehicle = await _context.DriverVehicles
                .FirstOrDefaultAsync(v => v.DriverId == driver.Id && v.IsActive);

            // ── Stats ─────────────────────────────────────────────────────
            var completedStops = allStops.Where(s =>
                s.StopStatus == HubStopStatus.Arrived ||
                s.StopStatus == HubStopStatus.Completed).ToList();

            var model = new DriverLogsViewModel
            {
                Driver = driver,
                Vehicle = vehicle,
                ActiveTab = tab,
                AllStops = allStops,
                DeliveryReports = deliveryReports,
                StatusLogs = statusLogs,
                Notifications = notifications,

                // Summary stats
                TotalAssigned = allStops.Count,
                TotalCompleted = completedStops.Count,
                TotalInTransit = allStops.Count(s => s.StopStatus == HubStopStatus.InTransit),
                TotalPending = allStops.Count(s => s.StopStatus == HubStopStatus.Pending),
                TotalReports = deliveryReports.Count,
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
                    (s.AssignedDriverId == driver.Id || s.OutgoingDriverId == driver.Id) && // ← ADD OutgoingDriverId
                    s.RouteAssignment.Order.OrderNumber == orderNumber &&
                    (s.StopStatus == HubStopStatus.Pending ||
                     s.StopStatus == HubStopStatus.InTransit));

            if (stop == null)
                return Json(new { found = false, message = "No active assignment found for this order." });

            // Resolve the correct label for this driver's role on the stop
            bool isOutgoingDriver = stop.OutgoingDriverId == driver.Id;
            string driverLabel = isOutgoingDriver
                ? (stop.OutgoingDriverLabel ?? "motorcycle driver")
                : (stop.DriverLabel ?? stop.HubLabel);

            bool isFinalLeg = stop.DriverLabel == "motorcycle driver";

            return Json(new
            {
                found = true,
                stopId = stop.Id,
                orderId = stop.RouteAssignment.Order.Id,  // ← ADD THIS
                orderNumber = stop.RouteAssignment.Order.OrderNumber,
                hubLabel = stop.HubLabel,
                driverLabel,
                status = stop.StopStatus.ToString(),
                buyerName = stop.RouteAssignment.Order.Buyer?.FullName,
                address = stop.RouteAssignment.ResolvedAddress
                              ?? stop.RouteAssignment.Order.DeliveryAddress,
                isFinalLeg
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
                    s.Id == stopId &&
                    (s.AssignedDriverId == driver.Id || s.OutgoingDriverId == driver.Id));

            if (stop == null) return NotFound();

            var order = stop.RouteAssignment.Order;
            //bool isFinalLeg = stop.OutgoingDriverId == driver.Id;
            bool isFinalLeg = stop.DriverLabel == "motorcycle driver";
            bool isFirstLeg = stop.StopOrder == 0;

            if (action == "start" && stop.StopStatus == HubStopStatus.Pending)
            {
                stop.StopStatus = HubStopStatus.InTransit;
                stop.DepartedFromPrevAt = DateTime.UtcNow;

                if (isFinalLeg)
                {
                    // The stop will be marked Completed only when the motorcycle
                    // driver scans "arrived" at the buyer's location.

                    order.Status = OrderStatus.OutForDelivery;
                    order.StatusLogs.Add(new OrderStatusLog
                    {
                        Status = OrderStatus.OutForDelivery,
                        Note = OrderStatus.ScanOutForDelivery(driver.FullName),
                        UpdatedByUserId = driver.Id,
                        UpdatedByName = driver.FullName,
                        IsVisibleToBuyer = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    TempData["Success"] = $"Out for delivery started for {order.OrderNumber}.";
                    await _activity.LogAsync(User, UserAction.OrderScanned,   // ← ADD
                        $"Driver started final leg (out for delivery) for Order {order.OrderNumber}.",
                        "Order", order.OrderNumber);
                }
                else
                {
                    // Mark the previous hub stop Completed — van has departed from it
                    var prevStop = await _context.OrderHubStops
                        .FirstOrDefaultAsync(s =>
                            s.OrderRouteAssignmentId == stop.OrderRouteAssignmentId &&
                            s.StopOrder == stop.StopOrder - 1);

                    if (prevStop != null && prevStop.StopStatus == HubStopStatus.Arrived)
                        prevStop.StopStatus = HubStopStatus.Completed;
                        
                    // Approved → InTransit only on the first van leg scan
                    // Subsequent legs: order is already InTransit, leave it alone
                    if (order.Status == OrderStatus.Approved)
                        order.Status = OrderStatus.InTransit;

                    string note = isFirstLeg
                        ? OrderStatus.ScanLeftWarehouse(driver.FullName)
                        : prevStop != null
                            ? OrderStatus.ScanLeftHub(prevStop.HubLabel, driver.FullName)
                            : OrderStatus.ScanLeftHub(stop.HubLabel, driver.FullName);

                    order.StatusLogs.Add(new OrderStatusLog
                    {
                        Status = OrderStatus.InTransit,
                        Note = note,
                        UpdatedByUserId = driver.Id,
                        UpdatedByName = driver.FullName,
                        IsVisibleToBuyer = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    TempData["Success"] = $"Trip started for {order.OrderNumber}.";
                    await _activity.LogAsync(User, UserAction.OrderScanned,   // ← ADD
                        $"Driver started leg {stop.StopOrder + 1} for Order {order.OrderNumber}.",
                        "Order", order.OrderNumber);
                }
            }
            else if (action == "arrived" && stop.StopStatus == HubStopStatus.InTransit)
            {
                stop.ArrivedAt = DateTime.UtcNow;

                if (isFinalLeg)
                {
                    // ✅ Don't complete yet — redirect to photo confirmation page
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ConfirmDelivery", new { stopId = stop.Id });
                }
                else
                {
                    stop.StopStatus = HubStopStatus.Arrived;

                    // Buyer-visible
                    order.StatusLogs.Add(new OrderStatusLog
                    {
                        Status = "Arrived at Hub",
                        Note = OrderStatus.ScanArrivedHub(stop.HubLabel, driver.FullName),
                        UpdatedByUserId = driver.Id,
                        UpdatedByName = driver.FullName,
                        IsVisibleToBuyer = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Internal only
                    order.StatusLogs.Add(new OrderStatusLog
                    {
                        Status = "Hub Arrived",
                        Note = $"Driver {driver.FullName} arrived at {stop.HubLabel}. Awaiting next leg assignment.",
                        UpdatedByUserId = driver.Id,
                        UpdatedByName = driver.FullName,
                        IsVisibleToBuyer = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    await NotifyHubStaffOnArrivalAsync(stop, order);
                    TempData["Success"] = $"Marked arrived at {stop.HubLabel}. Hub staff has been notified.";
                    await _activity.LogAsync(User, UserAction.OrderScanned,   // ← ADD
                        $"Driver arrived at {stop.HubLabel} for Order {order.OrderNumber}.",
                        "Order", order.OrderNumber);
                }
            }

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmDelivery(int stopId)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Forbid();

            var stop = await _context.OrderHubStops
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.Buyer)
                .FirstOrDefaultAsync(s => s.Id == stopId && s.AssignedDriverId == driver.Id);

            if (stop == null) return NotFound();

            ViewBag.Stop = stop;
            ViewBag.OrderNumber = stop.RouteAssignment.Order.OrderNumber;
            ViewBag.BuyerName = stop.RouteAssignment.Order.Buyer?.FullName;
            return View();   // ConfirmDelivery.cshtml — photo upload form
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelivery(int stopId, IFormFile photo, string? notes)
        {
            var driver = await _userManager.GetUserAsync(User);
            if (driver == null) return Forbid();

            var stop = await _context.OrderHubStops
                .Include(s => s.RouteAssignment)
                    .ThenInclude(r => r.Order)
                        .ThenInclude(o => o.StatusLogs)
                .FirstOrDefaultAsync(s => s.Id == stopId && s.AssignedDriverId == driver.Id);

            if (stop == null) return NotFound();
            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "A photo is required to confirm delivery.";
                return RedirectToAction("ConfirmDelivery", new { stopId });
            }

            // Save photo to wwwroot/delivery-photos/
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(photo.FileName)}";
            var savePath = Path.Combine(Directory.GetCurrentDirectory(),
                                        "wwwroot", "delivery-photos", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            using (var stream = new FileStream(savePath, FileMode.Create))
                await photo.CopyToAsync(stream);

            // Save report
            _context.RiderDeliveredReports.Add(new RiderDeliveredReport
            {
                OrderId = stop.RouteAssignment.OrderId,
                HubStopId = stop.Id,
                DriverId = driver.Id,
                PhotoPath = $"/delivery-photos/{fileName}",
                Notes = notes,
                DeliveredAt = DateTime.UtcNow
            });

            // NOW mark as completed
            var order = stop.RouteAssignment.Order;
            stop.StopStatus = HubStopStatus.Completed;
            order.Status = OrderStatus.Delivered;

            // ✅ Also complete the parent hub stop (the Arrived one)
            var parentHubStop = await _context.OrderHubStops
                .FirstOrDefaultAsync(s =>
                    s.OrderRouteAssignmentId == stop.OrderRouteAssignmentId &&
                    s.StopOrder == stop.StopOrder - 1 &&
                    s.StopStatus == HubStopStatus.Arrived);

            if (parentHubStop != null)
                parentHubStop.StopStatus = HubStopStatus.Completed;

            order.StatusLogs.Add(new OrderStatusLog
            {
                Status = OrderStatus.Delivered,
                Note = "Your parcel has been delivered. Thank you!",
                UpdatedByUserId = driver.Id,
                UpdatedByName = driver.FullName,
                IsVisibleToBuyer = true,
                CreatedAt = DateTime.UtcNow
            });

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _activity.LogAsync(User, UserAction.OrderScanned,          // ← ADD
                $"Order {order.OrderNumber} delivered with photo proof.",
                "Order", order.OrderNumber);

            TempData["Success"] = $"Order {order.OrderNumber} marked as delivered with photo proof.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> GetStaticRouteMapUrl(
    double fromLng, double fromLat,
    double toLng, double toLat,
    string token)
        {
            try
            {
                var url = $"https://api.mapbox.com/directions/v5/mapbox/driving/" +
                          $"{fromLng.ToString("F6", CultureInfo.InvariantCulture)}," +
                          $"{fromLat.ToString("F6", CultureInfo.InvariantCulture)};" +
                          $"{toLng.ToString("F6", CultureInfo.InvariantCulture)}," +
                          $"{toLat.ToString("F6", CultureInfo.InvariantCulture)}" +
                          $"?geometries=polyline&overview=full&access_token={token}";

                using var http = new HttpClient();
                var res = await http.GetStringAsync(url);
                var json = System.Text.Json.JsonDocument.Parse(res);
                var poly = json.RootElement
                               .GetProperty("routes")[0]
                               .GetProperty("geometry")
                               .GetString();

                if (string.IsNullOrEmpty(poly)) return null;

                var encodedPoly = Uri.EscapeDataString(poly);
                var hubLng = fromLng.ToString("F6", CultureInfo.InvariantCulture);
                var hubLat = fromLat.ToString("F6", CultureInfo.InvariantCulture);
                var dstLng = toLng.ToString("F6", CultureInfo.InvariantCulture);
                var dstLat = toLat.ToString("F6", CultureInfo.InvariantCulture);

                var markers = $"pin-s-l+1a73e8({hubLng},{hubLat}),pin-s-h+e53935({dstLng},{dstLat})";
                var path = $"path-3+1a73e8-0.8({encodedPoly})";

                return $"https://api.mapbox.com/styles/v1/mapbox/streets-v12/static/" +
                       $"{path},{markers}/auto/600x380@2x?padding=60&access_token={token}";
            }
            catch { return null; }
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
                        .ThenInclude(s => s.AssignedDriver)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Find this driver's stop
            var myStop = order.RouteAssignment?.HubStops
                .FirstOrDefault(s => s.AssignedDriverId == driver.Id);

            if (myStop == null) return Forbid();

            bool isMoto = myStop.DriverLabel == "motorcycle driver";

            // Get last hub coordinates
            double lastHubLat = DeliveryHubInfo.WarehouseLatitude;
            double lastHubLng = DeliveryHubInfo.WarehouseLongitude;
            string lastHubLabel = DeliveryHubInfo.WarehouseLabel;

            if (isMoto)
            {
                // The hub this moto stop departs from
                var parentStop = order.RouteAssignment!.HubStops
                    .Where(s => s.DriverLabel != "motorcycle driver")
                    .OrderByDescending(s => s.StopOrder)
                    .FirstOrDefault();

                if (parentStop != null)
                {
                    var hubInfo = DeliveryHubInfo.Hubs[parentStop.Hub];
                    lastHubLat = hubInfo.Lat;
                    lastHubLng = hubInfo.Lng;
                    lastHubLabel = hubInfo.Label;
                }
            }

            // Destination coords
            double? destLat = order.DeliveryLatitude
                ?? (double?)(order.RouteAssignment?.ResolvedLatitude != null
                    ? (double)order.RouteAssignment.ResolvedLatitude : null);
            double? destLng = order.DeliveryLongitude
                ?? (double?)(order.RouteAssignment?.ResolvedLongitude != null
                    ? (double)order.RouteAssignment.ResolvedLongitude : null);

            // Delivery report
            var report = isMoto
                ? await _context.RiderDeliveredReports
                    .FirstOrDefaultAsync(r => r.HubStopId == myStop.Id)
                : null;

            // In OrderDetail action, after building the model
            string? staticMapUrl = null;
            if (isMoto && order.Status == OrderStatus.Delivered
                && destLat.HasValue && destLng.HasValue)
            {
                staticMapUrl = await GetStaticRouteMapUrl(
                    lastHubLng, lastHubLat,
                    destLng.Value, destLat.Value,
                    _config["Mapbox:AccessToken"] ?? string.Empty);
            }

            var model = new DriverOrderDetailViewModel
            {
                Order = order,
                MyStop = myStop,
                IsMotoDriver = isMoto,
                MapboxToken = _config["Mapbox:AccessToken"] ?? string.Empty,
                LastHubLat = lastHubLat,
                LastHubLng = lastHubLng,
                LastHubLabel = lastHubLabel,
                DestLat = destLat,
                DestLng = destLng,
                DestAddress = order.RouteAssignment?.ResolvedAddress ?? order.DeliveryAddress,
                DeliveryReport = report,
                StaticRouteMapUrl = staticMapUrl
            };

            return View(model);
        }
    }
}