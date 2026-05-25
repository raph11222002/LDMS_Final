using LDMS_Final.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.SuperAdmin)]
    public class SystemUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public SystemUsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}