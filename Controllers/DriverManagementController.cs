using LDMS_Final.Data;
using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.LogisticStaff}")]
    public class DriverManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DriverManagementController(UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ── Shared helper: resolves company admin ID for both Admin and LogisticStaff ──
        private async Task<(ApplicationUser? currentUser, string? companyAdminId)> GetContextAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return (null, null);

            var companyAdminId = await _userManager.IsInRoleAsync(currentUser, RoleNames.Admin)
                ? currentUser.Id
                : currentUser.ParentAdminId;

            return (currentUser, companyAdminId);
        }

        // ── Shared helper: finds a driver and validates ownership ──
        private async Task<(ApplicationUser? driver, IActionResult? error)> GetValidDriverAsync(string id, string companyAdminId)
        {
            var driver = await _userManager.FindByIdAsync(id);
            if (driver == null)
                return (null, NotFound());

            if (driver.ParentAdminId != companyAdminId)
                return (null, Forbid());

            if (!await _userManager.IsInRoleAsync(driver, RoleNames.Driver))
                return (null, Forbid());

            return (driver, null);
        }

        // ── Index ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var (currentUser, companyAdminId) = await GetContextAsync();
            if (currentUser == null || string.IsNullOrWhiteSpace(companyAdminId))
                return Forbid();

            var driverRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.Driver)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(driverRoleId))
                return View(new List<DriverListItemViewModel>());

            var driverList = await (
                from u in _context.Users
                join ur in _context.UserRoles on u.Id equals ur.UserId
                where u.AssignedHub == currentUser.AssignedHub
                      && ur.RoleId == driverRoleId
                orderby u.CreatedAt descending
                select new DriverListItemViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    FullName = u.FullName ?? string.Empty,
                    ContactNumber = u.ContactNumber ?? string.Empty,
                    Gender = u.Gender ?? string.Empty,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                }
            ).ToListAsync();

            return View(driverList);
        }

        // ── Details ────────────────────────────────────────────
        public async Task<IActionResult> Details(string id)
        {
            var (_, companyAdminId) = await GetContextAsync();
            if (string.IsNullOrWhiteSpace(companyAdminId)) return Forbid();

            var (driver, error) = await GetValidDriverAsync(id, companyAdminId);
            if (error != null) return error;

            var model = new DriverListItemViewModel
            {
                Id = driver!.Id,
                UserName = driver.UserName ?? string.Empty,
                FullName = driver.FullName ?? string.Empty,
                ContactNumber = driver.ContactNumber ?? string.Empty,
                Gender = driver.Gender ?? string.Empty,
                IsActive = driver.IsActive,
                CreatedAt = driver.CreatedAt
            };

            return View(model);
        }

        // ── Create ─────────────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.LogisticStaff}")]
        public IActionResult Create() => View(new CreateUserViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.LogisticStaff}")]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (currentUser, companyAdminId) = await GetContextAsync();
            if (currentUser == null || string.IsNullOrWhiteSpace(companyAdminId)) return Forbid();

            DeliveryHub? assignedHub;
            if (await _userManager.IsInRoleAsync(currentUser, RoleNames.Admin))
                assignedHub = model.AssignedHub;
            else
                assignedHub = currentUser.AssignedHub;

            var userName = model.UserName.Trim().Replace(" ", "");

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = $"{userName.ToLowerInvariant()}@ldms.com",
                FullName = model.FullName.Trim(),
                ContactNumber = model.ContactNumber.Trim(),
                Gender = model.Gender,
                IsActive = true,
                ParentAdminId = companyAdminId,
                CreatedByUserId = currentUser.Id,
                AssignedHub = assignedHub,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, RoleNames.Driver);

                // Save vehicle if provided
                if (!string.IsNullOrWhiteSpace(model.VehicleType)
                    && !string.IsNullOrWhiteSpace(model.PlateNumber))
                {
                    _context.DriverVehicles.Add(new DriverVehicle
                    {
                        DriverId = user.Id,
                        VehicleType = model.VehicleType,
                        PlateNumber = model.PlateNumber.Trim().ToUpperInvariant(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Driver account '{user.FullName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ── Edit ───────────────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.LogisticStaff}")]
        public async Task<IActionResult> Edit(string id)
        {
            var (_, companyAdminId) = await GetContextAsync();
            if (string.IsNullOrWhiteSpace(companyAdminId)) return Forbid();

            var (driver, error) = await GetValidDriverAsync(id, companyAdminId);
            if (error != null) return error;

            return View(new EditUserViewModel
            {
                Id = driver!.Id,
                UserName = driver.UserName ?? string.Empty,
                FullName = driver.FullName ?? string.Empty,
                ContactNumber = driver.ContactNumber ?? string.Empty,
                Gender = driver.Gender ?? string.Empty,
                IsActive = driver.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.LogisticStaff}")]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (_, companyAdminId) = await GetContextAsync();
            if (string.IsNullOrWhiteSpace(companyAdminId)) return Forbid();

            var (driver, error) = await GetValidDriverAsync(model.Id, companyAdminId);
            if (error != null) return error;

            var cleanUserName = model.UserName.Trim().Replace(" ", "");
            driver!.UserName = cleanUserName;
            driver.Email = $"{cleanUserName.ToLowerInvariant()}@ldms.com";
            driver.FullName = model.FullName.Trim();
            driver.ContactNumber = model.ContactNumber.Trim();
            driver.Gender = model.Gender.Trim();
            driver.IsActive = model.IsActive;
            driver.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(driver);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Driver account '{driver.FullName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var identityError in result.Errors)
                ModelState.AddModelError(string.Empty, identityError.Description);

            return View(model);
        }

        // ── Toggle Status ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.LogisticStaff}")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var (_, companyAdminId) = await GetContextAsync();
            if (string.IsNullOrWhiteSpace(companyAdminId)) return Forbid();

            var (driver, error) = await GetValidDriverAsync(id, companyAdminId);
            if (error != null) return error;

            driver!.IsActive = !driver.IsActive;
            driver.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(driver);

            TempData["Success"] = $"'{driver.FullName}' has been {(driver.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        // ── Delete ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.LogisticStaff}")]
        public async Task<IActionResult> Delete(string id)
        {
            var (_, companyAdminId) = await GetContextAsync();
            if (string.IsNullOrWhiteSpace(companyAdminId)) return Forbid();

            var (driver, error) = await GetValidDriverAsync(id, companyAdminId);
            if (error != null) return error;

            await _userManager.DeleteAsync(driver!);

            TempData["Success"] = "Driver account deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}