using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LDMS_Final.Controllers
{
    [AllowAnonymous]
    [Route("[controller]")]
    public class RegistrationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public RegistrationController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet("Register")]
        [HttpGet("")]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost("Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim().ToLower();

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                ModelState.AddModelError(string.Empty, "This email is already registered.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = model.FullName.Trim(),
                ContactNumber = model.ContactNumber.Trim(),
                Gender = model.Gender.Trim(),
                AddressLine1 = model.AddressLine1.Trim(),
                AddressLine2 = model.AddressLine2?.Trim(),
                City = model.City.Trim(),
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            await _userManager.AddToRoleAsync(user, RoleNames.Buyer);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "BuyerHome");
        }
    }
}