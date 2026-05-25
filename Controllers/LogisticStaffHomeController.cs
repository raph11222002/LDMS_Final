using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.LogisticStaff)]
    public class LogisticStaffHomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LogisticStaffHomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        private async Task<List<ApplicationUser>> GetManagedDriversAsync()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null || string.IsNullOrEmpty(currentUser.ParentAdminId))
                return new List<ApplicationUser>();

            var driverRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.Driver)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(driverRoleId))
                return new List<ApplicationUser>();

            return await (
                from u in _context.Users
                join ur in _context.UserRoles on u.Id equals ur.UserId
                where u.AssignedHub == currentUser.AssignedHub
                      && ur.RoleId == driverRoleId
                orderby u.FullName
                select u
            )
            .Distinct()
            .ToListAsync();
        }

        public async Task<IActionResult> Index()
        {
            var drivers = await GetManagedDriversAsync();

            var model = new LogisticStaffDashboardViewModel
            {
                TotalDriverCount = drivers.Count,
                ActiveDriverCount = drivers.Count(x => x.IsActive),
                InactiveDriverCount = drivers.Count(x => !x.IsActive)
            };

            return View(model);
        }
    }
}