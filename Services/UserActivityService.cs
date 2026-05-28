using LDMS_Final.Data;
using LDMS_Final.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LDMS_Final.Services
{
    /// <summary>
    /// Injected into controllers to write one UserActivityLog row per action.
    ///
    /// Typical usage inside an action method:
    ///   await _activity.LogAsync(User, UserAction.OrderApproved,
    ///       $"Order {order.OrderNumber} approved.", "Order", order.OrderNumber);
    /// </summary>
    public class UserActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserActivityService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ── Primary overload: use inside any [Authorize] action ───────────
        public async Task LogAsync(
            ClaimsPrincipal principal,
            string action,
            string? description = null,
            string? entityType = null,
            string? entityId = null)
        {
            var appUser = await _userManager.GetUserAsync(principal);
            if (appUser == null) return;

            var roles = await _userManager.GetRolesAsync(appUser);
            var role = roles.FirstOrDefault() ?? "Unknown";

            Write(appUser.Id, appUser.UserName ?? string.Empty,
                  appUser.FullName, role,
                  action, description, entityType, entityId);

            await _context.SaveChangesAsync();
        }

        // ── Overload: when you already have the ApplicationUser loaded ────
        public async Task LogAsync(
            ApplicationUser appUser,
            string role,
            string action,
            string? description = null,
            string? entityType = null,
            string? entityId = null)
        {
            Write(appUser.Id, appUser.UserName ?? string.Empty,
                  appUser.FullName, role,
                  action, description, entityType, entityId);

            await _context.SaveChangesAsync();
        }

        // ── Overload: when you only have the userId string ────────────────────
        public async Task LogAsync(
            string userId,
            string action,
            string? description = null,
            string? entityType = null,
            string? entityId = null)
        {
            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser == null) return;

            var roles = await _userManager.GetRolesAsync(appUser);
            var role = roles.FirstOrDefault() ?? "Unknown";

            Write(appUser.Id, appUser.UserName ?? string.Empty,
                  appUser.FullName, role,
                  action, description, entityType, entityId);

            await _context.SaveChangesAsync();
        }

        // ── Internal writer ───────────────────────────────────────────────
        private void Write(
            string userId, string userName, string fullName, string role,
            string action, string? description, string? entityType, string? entityId)
        {
            _context.UserActivityLogs.Add(new UserActivityLog
            {
                UserId = userId,
                UserName = userName,
                FullName = fullName,
                Role = role,
                Action = action,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}