using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LDMS_Final.Controllers
{
    [Authorize(Roles = RoleNames.SuperAdmin)]
    public class OtherUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public OtherUsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, string? role)
        {
            var excludedRoles = new[] { RoleNames.Admin, RoleNames.SuperAdmin };

            var allUsers = _userManager.Users
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            var model = new List<OtherUserListItemViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Any(r => excludedRoles.Contains(r)))
                    continue;

                var userRole = roles.FirstOrDefault() ?? "No Role";

                model.Add(new OtherUserListItemViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    ContactNumber = user.ContactNumber,
                    Role = userRole,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            }

            // Filter by role
            if (!string.IsNullOrWhiteSpace(role))
                model = model.Where(x => x.Role.Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();

            // Filter by search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                model = model.Where(x =>
                    x.FullName.ToLower().Contains(s) ||
                    x.UserName.ToLower().Contains(s) ||
                    x.Email.ToLower().Contains(s)
                ).ToList();
            }

            ViewBag.Search = search;
            ViewBag.SelectedRole = role;
            ViewBag.AvailableRoles = model.Select(x => x.Role).Distinct().OrderBy(x => x).ToList();

            return View("~/Views/SuperAdmin/OtherUsers/Index.cshtml", model);
        }
    }
}