using LDMS_Final.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.SuperAdmin)]
    public class BuyerManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public BuyerManagementController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var buyers = await _userManager.GetUsersInRoleAsync(RoleNames.Buyer);

            return View(buyers.OrderByDescending(x => x.CreatedAt));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var buyer = await _userManager.FindByIdAsync(id);

            if (buyer == null)
                return NotFound();

            if (!await _userManager.IsInRoleAsync(buyer, RoleNames.Buyer))
                return Forbid();

            buyer.IsActive = true;
            buyer.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(buyer);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var buyer = await _userManager.FindByIdAsync(id);

            if (buyer == null)
                return NotFound();

            if (!await _userManager.IsInRoleAsync(buyer, RoleNames.Buyer))
                return Forbid();

            buyer.IsActive = false;
            buyer.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(buyer);

            return RedirectToAction(nameof(Index));
        }
    }
}