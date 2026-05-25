using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.SuperAdmin)]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ── Dashboard home ─────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            var buyers = await _userManager.GetUsersInRoleAsync(RoleNames.Buyer);

            var todayUtc = DateTime.UtcNow.Date;

            var model = new SystemAdminDashboardViewModel
            {
                AdminCount = admins.Count,
                BuyerCount = buyers.Count,
                ActiveAdminCount = admins.Count(x => x.IsActive),
                TotalLogsToday = await _context.UserActivityLogs
                                        .CountAsync(l => l.CreatedAt >= todayUtc),
                RecentActivity = await _context.UserActivityLogs
                                        .OrderByDescending(l => l.CreatedAt)
                                        .Take(6)
                                        .ToListAsync()
            };

            return View(model);
        }

        // ── User Logs ──────────────────────────────────────────────────────
        public async Task<IActionResult> UserLogs(
            string? searchName,
            string? searchUserId,
            string? filterRole,
            string? filterAction,
            string? dateFrom,
            string? dateTo,
            int page = 1)
        {
            const int pageSize = 25;

            var query = _context.UserActivityLogs.AsQueryable();

            // Name search: matches FullName or UserName (case-insensitive)
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                var term = searchName.Trim().ToLower();
                query = query.Where(l =>
                    l.FullName.ToLower().Contains(term) ||
                    l.UserName.ToLower().Contains(term));
            }

            // User ID: partial match
            if (!string.IsNullOrWhiteSpace(searchUserId))
            {
                var term = searchUserId.Trim().ToLower();
                query = query.Where(l => l.UserId.ToLower().Contains(term));
            }

            // Role exact match
            if (!string.IsNullOrWhiteSpace(filterRole))
                query = query.Where(l => l.Role == filterRole);

            // Action exact match
            if (!string.IsNullOrWhiteSpace(filterAction))
                query = query.Where(l => l.Action == filterAction);

            // Date range (local date → UTC)
            if (!string.IsNullOrWhiteSpace(dateFrom) &&
                DateTime.TryParse(dateFrom, out var dtFrom))
                query = query.Where(l => l.CreatedAt >= dtFrom.ToUniversalTime());

            if (!string.IsNullOrWhiteSpace(dateTo) &&
                DateTime.TryParse(dateTo, out var dtTo))
                query = query.Where(l => l.CreatedAt < dtTo.ToUniversalTime().AddDays(1));

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Populate dropdown options from what's already in the table
            var allRoles = await _context.UserActivityLogs
                .Select(l => l.Role).Distinct().OrderBy(r => r).ToListAsync();

            var allActions = await _context.UserActivityLogs
                .Select(l => l.Action).Distinct().OrderBy(a => a).ToListAsync();

            var model = new UserLogsViewModel
            {
                Logs = logs,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                SearchName = searchName,
                SearchUserId = searchUserId,
                FilterRole = filterRole,
                FilterAction = filterAction,
                DateFrom = dateFrom,
                DateTo = dateTo,
                AllRoles = allRoles,
                AllActions = allActions
            };

            return View(model);
        }

        // ── AJAX: single log entry for the detail modal ────────────────────
        [HttpGet]
        public async Task<IActionResult> LogDetail(int id)
        {
            var log = await _context.UserActivityLogs.FindAsync(id);
            if (log == null) return NotFound();

            return Json(new
            {
                log.Id,
                log.UserId,
                log.UserName,
                log.FullName,
                log.Role,
                log.Action,
                log.Description,
                log.EntityType,
                log.EntityId,
                CreatedAt = log.CreatedAt.ToString("o")   // ISO 8601 for JS Date parsing
            });
        }

        // ── CSV Export (same filters as the page) ─────────────────────────
        [HttpGet]
        public async Task<IActionResult> ExportLogs(
            string? searchName, string? searchUserId,
            string? filterRole, string? filterAction,
            string? dateFrom, string? dateTo)
        {
            var query = _context.UserActivityLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                var t = searchName.Trim().ToLower();
                query = query.Where(l => l.FullName.ToLower().Contains(t) || l.UserName.ToLower().Contains(t));
            }

            if (!string.IsNullOrWhiteSpace(searchUserId))
                query = query.Where(l => l.UserId.ToLower().Contains(searchUserId.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(filterRole))
                query = query.Where(l => l.Role == filterRole);

            if (!string.IsNullOrWhiteSpace(filterAction))
                query = query.Where(l => l.Action == filterAction);

            if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var df))
                query = query.Where(l => l.CreatedAt >= df.ToUniversalTime());

            if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var dt))
                query = query.Where(l => l.CreatedAt < dt.ToUniversalTime().AddDays(1));

            var logs = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,UserId,UserName,FullName,Role,Action,Description,EntityType,EntityId,DateTime(UTC)");

            foreach (var l in logs)
                sb.AppendLine(
                    $"{l.Id}," +
                    $"\"{l.UserId}\"," +
                    $"\"{Csv(l.UserName)}\"," +
                    $"\"{Csv(l.FullName)}\"," +
                    $"\"{l.Role}\"," +
                    $"\"{l.Action}\"," +
                    $"\"{Csv(l.Description ?? "")}\"," +
                    $"\"{l.EntityType ?? ""}\"," +
                    $"\"{l.EntityId ?? ""}\"," +
                    $"{l.CreatedAt:yyyy-MM-dd HH:mm:ss}");

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"user_activity_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private static string Csv(string s) => s.Replace("\"", "\"\"");
    }
}