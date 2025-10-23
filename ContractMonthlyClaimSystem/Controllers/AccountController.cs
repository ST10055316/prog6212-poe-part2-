using System;
using System.Collections.Generic;
using System.Security.Claims; // This now correctly refers to System.Security.Claims
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// REMOVED: using Claim = ContractMonthlyClaimSystem.Models.Claim; // This caused CS0104

namespace ContractMonthlyClaimSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public AccountController(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Find user by username
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username);

                    // Check password hash
                    if (user != null && user.PasswordHash == HashPassword(model.Password))
                    {
                        // Check if user is active
                        // FIX for CS0103/CS0117 (Assuming UserStatus is a separate accessible enum)
                        if (user.Status != UserStatus.Active)
                        {
                            ModelState.AddModelError(string.Empty, "Your account is not active. Please contact administrator.");
                            return View(model);
                        }

                        // Create claims
                        // FIX for CS0104: This now correctly uses System.Security.Claims.Claim
                        var claims = new List<System.Security.Claims.Claim>
                        {
                            new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new System.Security.Claims.Claim(ClaimTypes.Name, user.Name),
                            new System.Security.Claims.Claim(ClaimTypes.Email, user.Email),
                            new System.Security.Claims.Claim(ClaimTypes.Role, user.Role.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(60)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        // Redirect to Dashboard
                        return RedirectToAction("Dashboard", "Claim");
                    }

                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                }
                catch (Exception ex)
                {
                    // Log the error (you might want to use ILogger here)
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                }
            }

            return View(model);
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if username already exists
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                    if (existingUser != null)
                    {
                        if (existingUser.Username == model.Username)
                        {
                            ModelState.AddModelError("Username", "Username already exists.");
                        }
                        if (existingUser.Email == model.Email)
                        {
                            ModelState.AddModelError("Email", "Email already exists.");
                        }
                        return View(model);
                    }

                    // Create new user with all required fields
                    var user = new User
                    {
                        Username = model.Username,
                        Name = model.Name,
                        Email = model.Email,
                        PasswordHash = HashPassword(model.Password),
                        Role = UserRole.Lecturer, // Default role for new registrations
                        Department = model.Department ?? "General",
                        Status = UserStatus.Active, // FIX for CS0103/CS0117
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Registration failed: {ex.Message}");
                }
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Logout (for GET requests as well)
        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Use a better salt in production - consider using ASP.NET Core Identity instead
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SALT"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}