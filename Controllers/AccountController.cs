using LDMS_Final.Models;
using LDMS_Final.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LDMS_Final.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to correct dashboard
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToDashboard();

            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            // reCAPTCHA check — must come before ModelState check
            var recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
            if (string.IsNullOrEmpty(recaptchaToken) || !await VerifyRecaptchaAsync(recaptchaToken))
            {
                ViewData["RecaptchaError"] = "Please complete the CAPTCHA.";
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            var login = model.UserNameOrEmail.Trim();

            // Try email first, then username
            var user = await _userManager.FindByEmailAsync(login)
                    ?? await _userManager.FindByNameAsync(login);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your administrator.");
                return View(model);
            }

            // Always sign in using UserName (Identity requirement)
            // UserName is guaranteed non-null since we found the user
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);   // ← false prevents silent lockouts during testing

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains(RoleNames.SuperAdmin))
                    return RedirectToAction("Index", "Dashboard");

                if (roles.Contains(RoleNames.Admin))
                    return RedirectToAction("Index", "AdminHome");

                if (roles.Contains(RoleNames.LogisticStaff))
                    return RedirectToAction("Index", "LogisticStaffHome");

                if (roles.Contains(RoleNames.WarehouseStaff))
                    return RedirectToAction("Index", "WarehouseStaffHome");

                if (roles.Contains(RoleNames.Driver))
                    return RedirectToAction("Index", "DriverHome");

                if (roles.Contains(RoleNames.Buyer))
                    return RedirectToAction("Index", "BuyerHome");

                // Logged in but no recognized role
                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty, "Your account has no assigned role. Please contact support.");
                return View(model);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is temporarily locked. Please try again later.");
                return View(model);
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Login is not allowed for this account. Email may need confirmation.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ── Helper ─────────────────────────────────────────────
        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole(RoleNames.SuperAdmin))
                return RedirectToAction("Index", "Dashboard");
            if (User.IsInRole(RoleNames.Admin))
                return RedirectToAction("Index", "AdminHome");
            if (User.IsInRole(RoleNames.LogisticStaff))
                return RedirectToAction("Index", "LogisticStaffHome");
            if (User.IsInRole(RoleNames.WarehouseStaff))
                return RedirectToAction("Index", "WarehouseStaffHome");
            if (User.IsInRole(RoleNames.Driver))
                return RedirectToAction("Index", "DriverHome");
            if (User.IsInRole(RoleNames.Buyer))
                return RedirectToAction("Index", "BuyerHome");

            return RedirectToAction(nameof(Login));
        }

        private async Task<bool> VerifyRecaptchaAsync(string token)
        {
            var secretKey = _configuration["GoogleReCaptcha:SecretKey"];
            using var http = new HttpClient();
            var response = await http.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }
    }
}