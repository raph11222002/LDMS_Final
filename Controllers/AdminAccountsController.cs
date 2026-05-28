using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LDMS_Final.Services;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.SuperAdmin)]
    public class AdminAccountsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserActivityService _activity;

        public AdminAccountsController(UserManager<ApplicationUser> userManager, 
            UserActivityService activity)
        {
            _userManager = userManager;
            _activity = activity;
        }

        public async Task<IActionResult> Index()
        {
            var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);

            var model = admins
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdminListItemViewModel
                {
                    Id = x.Id,
                    UserName = x.UserName ?? string.Empty,
                    FullName = x.FullName,
                    ContactNumber = x.ContactNumber,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var normalizedUserName = model.UserName.Trim().Replace(" ", "");
            var syntheticEmail = $"{normalizedUserName.ToLowerInvariant()}@ldms.com";

            var admin = new ApplicationUser
            {
                UserName = normalizedUserName,
                Email = syntheticEmail,
                FullName = model.FullName.Trim(),
                ContactNumber = model.ContactNumber.Trim(),
                IsActive = true,
                EmailConfirmed = true,
                CreatedByUserId = _userManager.GetUserId(User),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(admin, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, RoleNames.Admin);
                TempData["Success"] = "Admin account created successfully.";
                await _activity.LogAsync(User, UserAction.AccountCreated,       // ← ADD
                    $"Admin account '{admin.UserName}' created.",
                    "Admin", admin.Id);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null || !await _userManager.IsInRoleAsync(admin, RoleNames.Admin))
                return NotFound();

            var model = new EditAdminViewModel
            {
                Id = admin.Id,
                UserName = admin.UserName ?? string.Empty,
                FullName = admin.FullName,
                ContactNumber = admin.ContactNumber,
                IsActive = admin.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAdminViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = await _userManager.FindByIdAsync(model.Id);

            if (admin == null || !await _userManager.IsInRoleAsync(admin, RoleNames.Admin))
                return NotFound();

            var newUserName = model.UserName.Trim().Replace(" ", "");
            var newEmail = $"{newUserName.ToLowerInvariant()}@ldms.com";

            admin.UserName = newUserName;
            admin.Email = newEmail;
            admin.FullName = model.FullName.Trim();
            admin.ContactNumber = model.ContactNumber.Trim();
            admin.IsActive = model.IsActive;
            admin.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(admin);

            if (result.Succeeded)
            {
                TempData["Success"] = "Admin account updated successfully.";
                await _activity.LogAsync(User, UserAction.AccountUpdated,       // ← ADD
                    $"Admin account '{admin.UserName}' updated.",
                    "Admin", admin.Id);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null || !await _userManager.IsInRoleAsync(admin, RoleNames.Admin))
                return NotFound();

            admin.IsActive = false;
            admin.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(admin);

            TempData["Success"] = "Admin account deactivated.";
            await _activity.LogAsync(User, UserAction.AccountToggled,       // ← ADD
                $"Admin account '{admin.UserName}' deactivated.",
                "Admin", admin.Id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);

            if (admin == null || !await _userManager.IsInRoleAsync(admin, RoleNames.Admin))
                return NotFound();

            admin.IsActive = true;
            admin.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(admin);

            TempData["Success"] = "Admin account activated.";
            await _activity.LogAsync(User, UserAction.AccountToggled,       // ← ADD
                $"Admin account '{admin.UserName}' activated.",
                "Admin", admin.Id);
            return RedirectToAction(nameof(Index));
        }
    }
}