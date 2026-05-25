using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminHomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminHomeController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentAdminId = _userManager.GetUserId(User);

            var logisticStaff = await _userManager.GetUsersInRoleAsync(RoleNames.LogisticStaff);
            var warehouseStaff = await _userManager.GetUsersInRoleAsync(RoleNames.WarehouseStaff);
            var allDrivers = await _userManager.GetUsersInRoleAsync(RoleNames.Driver);

            var myLogisticStaff = logisticStaff.Where(x => x.ParentAdminId == currentAdminId).ToList();
            var myWarehouseStaff = warehouseStaff.Where(x => x.ParentAdminId == currentAdminId).ToList();
            var myDrivers = allDrivers.Where(x => x.ParentAdminId == currentAdminId).ToList();

            var myProducts = await _context.Products
                .Where(x => x.CreatedByAdminId == currentAdminId)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalStaffCount = myLogisticStaff.Count + myWarehouseStaff.Count,
                ActiveStaffCount = myLogisticStaff.Count(x => x.IsActive) + myWarehouseStaff.Count(x => x.IsActive),
                LogisticStaffCount = myLogisticStaff.Count,
                WarehouseStaffCount = myWarehouseStaff.Count,
                TotalDriverCount = myDrivers.Count,
                ActiveDriverCount = myDrivers.Count(x => x.IsActive),
                TotalProductCount = myProducts.Count,
                ActiveProductCount = myProducts.Count(x => x.IsActive)
            };

            return View(model);
        }
    }
}