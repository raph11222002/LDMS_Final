using LDMS_Final.Services;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class StaffManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserActivityService _activity;

        public StaffManagementController(UserManager<ApplicationUser> userManager, UserActivityService activity)
        {
            _userManager = userManager;
            _activity = activity;
        }

        // ── Index ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var currentAdminId = _userManager.GetUserId(User);

            // Admin only sees logistic and warehouse staff, NOT drivers
            var staffs = await _userManager.Users
                .Where(x => x.ParentAdminId == currentAdminId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var staffWithRoles = new List<StaffListItemViewModel>();

            foreach (var staff in staffs)
            {
                var roles = await _userManager.GetRolesAsync(staff);
                var role = roles.FirstOrDefault() ?? "Unknown";

                // Skip drivers — admin cannot manage them
                if (role == RoleNames.Driver) continue;

                staffWithRoles.Add(new StaffListItemViewModel
                {
                    Id = staff.Id,
                    UserName = staff.UserName ?? string.Empty,
                    FullName = staff.FullName ?? string.Empty,
                    ContactNumber = staff.ContactNumber ?? string.Empty,
                    Gender = staff.Gender ?? string.Empty,
                    Role = role,

                    AssignedHub = staff.AssignedHub.HasValue
                    ? DeliveryHubInfo.Hubs[staff.AssignedHub.Value].ShortName
                    : null,

                    IsActive = staff.IsActive,
                    CreatedAt = staff.CreatedAt
                });
            }

            return View(staffWithRoles);
        }

        // ── Create ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentAdminId = _userManager.GetUserId(User);

            DeliveryHub? assignedHub = null;

            if (model.SelectedRole == RoleNames.LogisticStaff)
            {
                //if (model.SelectedHub == null)
                //{
                    //ModelState.AddModelError(nameof(model.SelectedHub), "Please select an assigned hub for Logistic Staff.");
                    //return View(model);
                //}

                assignedHub = model.SelectedHub;
            }

            // Admin can only create logistic and warehouse staff, NOT drivers
            var allowedRoles = new[] { RoleNames.LogisticStaff, RoleNames.WarehouseStaff };
            if (!allowedRoles.Contains(model.SelectedRole))
                return Forbid();

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = $"{model.UserName}@ldms.com",
                FullName = model.FullName,
                ContactNumber = model.ContactNumber,
                Gender = model.Gender,
                IsActive = true,
                ParentAdminId = currentAdminId,
                CreatedByUserId = currentAdminId,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
                AssignedHub = assignedHub
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);

                if (roleResult.Succeeded)
                {
                    await _activity.LogAsync(_userManager.GetUserId(User)!, UserAction.AccountCreated,
                        $"Staff account '{user.FullName}' ({model.SelectedRole}) created.",
                        "User", user.Id);
                        
                    TempData["Success"] = $"Staff account '{model.FullName}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ── Edit ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var currentAdminId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.ParentAdminId != currentAdminId)
                return NotFound();

            // Admin cannot edit drivers
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(RoleNames.Driver))
                return Forbid();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                ContactNumber = user.ContactNumber ?? string.Empty,
                Gender = user.Gender ?? string.Empty,
                IsActive = user.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var currentAdminId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null || user.ParentAdminId != currentAdminId)
                return NotFound();

            // Admin cannot edit drivers
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(RoleNames.Driver))
                return Forbid();

            user.UserName = model.UserName;
            user.FullName = model.FullName;
            user.ContactNumber = model.ContactNumber;
            user.Gender = model.Gender;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _activity.LogAsync(_userManager.GetUserId(User)!, UserAction.AccountUpdated,
                    $"Staff account '{user.FullName}' updated.",
                    "User", user.Id);
                    
                TempData["Success"] = $"Staff account '{user.FullName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ── Toggle Status ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var currentAdminId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.ParentAdminId != currentAdminId)
                return NotFound();

            // Admin cannot deactivate drivers
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(RoleNames.Driver))
                return Forbid();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _activity.LogAsync(_userManager.GetUserId(User)!, UserAction.AccountToggled,
                $"Staff account '{user.FullName}' {(user.IsActive ? "activated" : "deactivated")}.",
                "User", user.Id);

            TempData["Success"] = $"'{user.FullName}' has been {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        // ── Delete ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var currentAdminId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.ParentAdminId != currentAdminId)
                return NotFound();

            // Admin cannot delete drivers
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(RoleNames.Driver))
                return Forbid();

            await _userManager.DeleteAsync(user);

            await _activity.LogAsync(_userManager.GetUserId(User)!, UserAction.AccountDeleted,
                $"Staff account '{user.FullName}' deleted.",
                "User", id);

            TempData["Success"] = "Staff account deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}