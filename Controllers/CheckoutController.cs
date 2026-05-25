using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.Services;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.Buyer)]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OrderService _orderService;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            OrderService orderService)
        {
            _context = context;
            _userManager = userManager;
            _orderService = orderService;
        }

        private string GetBuyerId() => _userManager.GetUserId(User)!;

        private const double WarehouseLatitude = 7.036924878323465;
        private const double WarehouseLongitude = 125.52539152251724;

        // ── Hub in-city radius: buyer within 20 km of any hub gets motorcycle last-mile that also rate every 15km journey ──
        private const double InCityHubRadiusKm = 20.0;

        private enum DeliveryHub
        {
            Davao,
            Surigao,
            Tacloban,
            Pasay
        }

        // ── Hub coordinates ────────────────────────────────────
        private static (double Lat, double Lon) GetHubCoordinates(DeliveryHub hub) =>
            hub switch
            {
                DeliveryHub.Davao => (7.064764940987943, 125.60869053843403),
                DeliveryHub.Surigao => (9.790260868323017, 125.49312423896352),
                DeliveryHub.Tacloban => (11.222528827106181, 124.99200750511456),
                DeliveryHub.Pasay => (14.537682187625546, 121.00077350135048),
                _ => (14.537682187625546, 121.00077350135048)
            };

        // ── Main shipping fee entry point ──────────────────────
        private decimal CalculateShippingFee(
            IEnumerable<CartItemViewModel> items,
            double? latitude,
            double? longitude)
        {
            var totalWeight = items?.Sum(i => i.Weight * i.Quantity) ?? 0m;

            // Out-of-city: full multi-van route + last-mile
            return CalculateOutOfCityShippingFee(totalWeight, latitude, longitude);
        }

        // ── In-city motorcycle fee ─────────────────────────────
        // Base fee = 45, Extra weight fee = (weight – 1 minimum) × 10
        private static decimal CalculateInCityMotorcycleFee(decimal totalWeight)
        {
            const decimal motorcycleBase = 45m;
            var effectiveWeight = Math.Max(totalWeight, 1m);
            var extraWeight = effectiveWeight - 1m;
            return Math.Round(motorcycleBase + extraWeight * 10m, 2);
        }

        // ── Out-of-city: 4-van chain + last-mile ──────────────
        private decimal CalculateOutOfCityShippingFee(
            decimal totalWeight,
            double? latitude,
            double? longitude)
        {
            var (nearestHub, distanceToNearestHub) = GetNearestHub(latitude, longitude);

            // Van 1 – Warehouse → Davao Sorting Center  (always)
            //   Base fee = 20, Rate = 7 per kg
            decimal van1Cost = 20m + totalWeight * 7m;

            // Van 2 – Davao Sorting Center → Surigao Sorting Center
            //   Rate = 50 per kg  (skipped if nearest hub is Davao)
            decimal van2Cost = nearestHub == DeliveryHub.Davao
                ? 0m
                : totalWeight * 50m;

            // Van 3 – Surigao Sorting Center → Tacloban Sorting Center
            //   Rate = 50 per kg  (only when nearest hub is Tacloban or Pasay)
            decimal van3Cost = (nearestHub == DeliveryHub.Tacloban || nearestHub == DeliveryHub.Pasay)
                ? totalWeight * 50m
                : 0m;

            // Van 4 – Tacloban Sorting Center → Pasay Delivery Hub
            //   Rate = 65 per kg  (only when nearest hub is Pasay)
            decimal van4Cost = nearestHub == DeliveryHub.Pasay
                ? totalWeight * 65m
                : 0m;

            // Last-mile motorcycle from the nearest hub to buyer
            decimal lastMileCost = GetLastMileCost(nearestHub, totalWeight, latitude, longitude);

            return Math.Round(van1Cost + van2Cost + van3Cost + van4Cost + lastMileCost, 2);
        }

        // ── Last-mile motorcycle cost ──────────────────────────
        // Within 20 km of hub  → Base 45 + (weight – 1) × 10
        // Beyond 20 km from hub → Rate = 10/kg + 25 per 15 km block
        private decimal GetLastMileCost(
            DeliveryHub hub,
            decimal totalWeight,
            double? latitude,
            double? longitude)
        {
            if (!latitude.HasValue || !longitude.HasValue)
            {
                // No coordinates – apply in-city formula as a safe fallback
                return CalculateInCityMotorcycleFee(totalWeight);
            }

            var hubCoords = GetHubCoordinates(hub);
            var hubDistance = GetDistanceKm(hubCoords.Lat, hubCoords.Lon, latitude.Value, longitude.Value);

            if (hubDistance > InCityHubRadiusKm)
            {
                // Too far from hub: Rate = 10/kg + 25 per 15 km distance block
                var distanceBlocks = (decimal)Math.Ceiling(hubDistance / 15.0);
                return Math.Round(totalWeight * 10m + 25m * distanceBlocks, 2);
            }

            // Within 20 km of hub
            return CalculateInCityMotorcycleFee(totalWeight);
        }

        // ── Nearest hub helper ─────────────────────────────────
        private static (DeliveryHub Hub, double Distance) GetNearestHub(double? latitude, double? longitude)
        {
            if (!latitude.HasValue || !longitude.HasValue)
                return (DeliveryHub.Pasay, double.MaxValue);

            var hubs = new[]
            {
                DeliveryHub.Davao,
                DeliveryHub.Surigao,
                DeliveryHub.Tacloban,
                DeliveryHub.Pasay
            };

            var nearest = DeliveryHub.Pasay;
            var nearestDist = double.MaxValue;

            foreach (var h in hubs)
            {
                var coords = GetHubCoordinates(h);
                var dist = GetDistanceKm(coords.Lat, coords.Lon, latitude.Value, longitude.Value);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = h;
                }
            }

            return (nearest, nearestDist);
        }

        // ── IsNearAnyHub: buyer within 20 km of any hub ────────
        // Also accepts address fallback (checks if text contains a hub city name)
        private bool IsNearAnyHub(
            double? latitude,
            double? longitude,
            string deliveryAddress,
            out DeliveryHub nearestHub)
        {
            nearestHub = DeliveryHub.Pasay;

            if (latitude.HasValue && longitude.HasValue)
            {
                var (hub, dist) = GetNearestHub(latitude, longitude);
                nearestHub = hub;
                return dist <= InCityHubRadiusKm;
            }

            // Coordinate-free fallback: match hub city names in the address
            if (!string.IsNullOrWhiteSpace(deliveryAddress))
            {
                if (deliveryAddress.Contains("Davao", StringComparison.OrdinalIgnoreCase)) { nearestHub = DeliveryHub.Davao; return true; }
                if (deliveryAddress.Contains("Surigao", StringComparison.OrdinalIgnoreCase)) { nearestHub = DeliveryHub.Surigao; return true; }
                if (deliveryAddress.Contains("Tacloban", StringComparison.OrdinalIgnoreCase)) { nearestHub = DeliveryHub.Tacloban; return true; }
                if (deliveryAddress.Contains("Pasay", StringComparison.OrdinalIgnoreCase)) { nearestHub = DeliveryHub.Pasay; return true; }
                if (deliveryAddress.Contains("Manila", StringComparison.OrdinalIgnoreCase)) { nearestHub = DeliveryHub.Pasay; return true; }
            }

            return false;
        }

        // ── Haversine formula distance ─────────────────────────────────
        private static double GetDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            if (lat1 == lat2 && lon1 == lon2) return 0;
            const double R = 6371.0;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                    * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double angle) => angle * Math.PI / 180.0;

        // ── Checkout Page ──────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var buyerId = GetBuyerId();
            var user = await _userManager.GetUserAsync(User);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.BuyerId == buyerId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            var items = new List<CartItemViewModel>();
            foreach (var c in cartItems)
            {
                var stockQuery = _context.ProductStocks.Where(s => s.ProductId == c.ProductId);
                if (!string.IsNullOrEmpty(c.VariantName))
                    stockQuery = stockQuery.Where(s => s.VariantName == c.VariantName);
                else
                    stockQuery = stockQuery.Where(s => s.VariantName == null);
                if (!string.IsNullOrEmpty(c.SelectedSize))
                    stockQuery = stockQuery.Where(s => s.SelectedSize == c.SelectedSize);
                else
                    stockQuery = stockQuery.Where(s => s.SelectedSize == null);

                var stock = await stockQuery.FirstOrDefaultAsync();
                items.Add(new CartItemViewModel
                {
                    CartItemId = c.Id,
                    ProductId = c.ProductId,
                    ProductName = c.Product.Name,
                    VariantName = c.VariantName,
                    SelectedSize = c.SelectedSize,
                    ImagePath = c.Product.ImagePath1,
                    UnitPrice = c.Product.Price,
                    Weight = c.Product.Weight,
                    Quantity = c.Quantity,
                    AvailableStock = stock?.Quantity ?? 0
                });
            }

            var model = new CheckoutViewModel
            {
                Items = items,
                DeliveryFullName = user?.FullName ?? string.Empty,
                DeliveryContactNumber = user?.ContactNumber ?? string.Empty,
                DeliveryAddress = $"{user?.AddressLine1} {user?.AddressLine2}, {user?.City}".Trim()
            };

            model.ShippingFee = CalculateShippingFee(items, null, null);

            return View(model);
        }

        // ── Confirm Order ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(CheckoutViewModel model)
        {
            var buyerId = GetBuyerId();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.BuyerId == buyerId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            // ── Re-check inventory before confirming ──────────
            var stockErrors = new List<string>();

            foreach (var c in cartItems)
            {
                var stockQuery = _context.ProductStocks.Where(s => s.ProductId == c.ProductId);
                if (!string.IsNullOrEmpty(c.VariantName))
                    stockQuery = stockQuery.Where(s => s.VariantName == c.VariantName);
                else
                    stockQuery = stockQuery.Where(s => s.VariantName == null);
                if (!string.IsNullOrEmpty(c.SelectedSize))
                    stockQuery = stockQuery.Where(s => s.SelectedSize == c.SelectedSize);
                else
                    stockQuery = stockQuery.Where(s => s.SelectedSize == null);

                var stock = await stockQuery.FirstOrDefaultAsync();
                var availableQty = stock?.Quantity ?? 0;

                if (availableQty < c.Quantity)
                {
                    var label = c.Product.Name;
                    if (!string.IsNullOrEmpty(c.VariantName)) label += $" ({c.VariantName})";
                    if (!string.IsNullOrEmpty(c.SelectedSize)) label += $" / {c.SelectedSize}";
                    stockErrors.Add($"{label}: only {availableQty} left.");
                }
            }

            if (stockErrors.Any())
            {
                TempData["StockError"] = string.Join("|", stockErrors);
                var items = cartItems.Select(c => new CartItemViewModel
                {
                    CartItemId = c.Id,
                    ProductId = c.ProductId,
                    ProductName = c.Product.Name,
                    VariantName = c.VariantName,
                    SelectedSize = c.SelectedSize,
                    ImagePath = c.Product.ImagePath1,
                    UnitPrice = c.Product.Price,
                    Weight = c.Product.Weight,
                    Quantity = c.Quantity
                }).ToList();
                model.Items = items;
                model.ShippingFee = CalculateShippingFee(items, model.DeliveryLatitude, model.DeliveryLongitude);
                return View("Index", model);
            }

            var shippingFee = CalculateShippingFee(
                cartItems.Select(c => new CartItemViewModel
                {
                    Weight = c.Product.Weight,
                    Quantity = c.Quantity
                }),
                model.DeliveryLatitude,
                model.DeliveryLongitude);

            // ── Create Order ───────────────────────────────────
            var adminId = cartItems.First().Product.CreatedByAdminId;
            var orderNumber = await _orderService.GenerateOrderNumberAsync();

            var order = new Order
            {
                BuyerId = buyerId,
                OrderNumber = orderNumber,
                TotalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity) + shippingFee,
                ShippingFee = shippingFee,
                PaymentMethod = model.PaymentMethod,
                Status = OrderStatus.Pending,
                DeliveryFullName = model.DeliveryFullName,
                DeliveryContactNumber = model.DeliveryContactNumber,
                DeliveryAddress = model.DeliveryAddress,
                DeliveryNote = model.DeliveryNote,
                DeliveryLatitude = model.DeliveryLatitude,
                DeliveryLongitude = model.DeliveryLongitude,
                CreatedByAdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                Items = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    ProductName = c.Product.Name,
                    VariantName = c.VariantName,
                    SelectedSize = c.SelectedSize,
                    UnitPrice = c.Product.Price,
                    Quantity = c.Quantity
                }).ToList(),
                StatusLogs = new List<OrderStatusLog>
                {
                    new OrderStatusLog
                    {
                        Status          = OrderStatus.Pending,
                        Note            = "Order placed and waiting to be approved.",
                        UpdatedByUserId = buyerId,
                        UpdatedByName   = model.DeliveryFullName,
                        IsVisibleToBuyer = true,
                        CreatedAt       = DateTime.UtcNow
                    }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ── Deduct stock & log ─────────────────────────────
            foreach (var c in cartItems)
            {
                var stockQuery = _context.ProductStocks.Where(s => s.ProductId == c.ProductId);
                if (!string.IsNullOrEmpty(c.VariantName))
                    stockQuery = stockQuery.Where(s => s.VariantName == c.VariantName);
                else
                    stockQuery = stockQuery.Where(s => s.VariantName == null);
                if (!string.IsNullOrEmpty(c.SelectedSize))
                    stockQuery = stockQuery.Where(s => s.SelectedSize == c.SelectedSize);
                else
                    stockQuery = stockQuery.Where(s => s.SelectedSize == null);

                var stock = await stockQuery.FirstOrDefaultAsync();
                if (stock != null)
                {
                    var previous = stock.Quantity;
                    stock.Quantity = Math.Max(0, stock.Quantity - c.Quantity);
                    stock.UpdatedAt = DateTime.UtcNow;
                    stock.UpdatedByUserId = buyerId;

                    _context.ProductStockLogs.Add(new ProductStockLog
                    {
                        ProductId = c.ProductId,
                        VariantName = c.VariantName,
                        SelectedSize = c.SelectedSize,
                        PreviousQuantity = previous,
                        NewQuantity = stock.Quantity,
                        UpdatedByUserId = buyerId,
                        UpdatedByName = model.DeliveryFullName,
                        UpdatedAt = DateTime.UtcNow,
                        Note = $"Order {orderNumber} placed"
                    });
                }
            }

            // Generate QR and shipping label
            var qrPath = _orderService.GenerateQrCode(orderNumber);
            order.QrCodePath = qrPath;
            var totalWeightKg = cartItems.Sum(c => c.Product.Weight * c.Quantity);
            order.ShippingLabelPath = _orderService.GenerateShippingLabelPdf(order, totalWeightKg);

            // Clear cart
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderSuccess", new { orderNumber });
        }

        // ── Order Success ──────────────────────────────────────
        public IActionResult OrderSuccess(string orderNumber)
        {
            ViewBag.OrderNumber = orderNumber;
            return View();
        }
    }
}
