using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Diagnostics;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using Microsoft.Extensions.Logging;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IClaimService claimService, IUserService userService, ILogger<HomeController> logger)
        {
            _claimService = claimService;
            _userService = userService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Redirect to the Claim Dashboard
            return RedirectToAction("Dashboard", "Claim");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Alternative method if you want to redirect to the Claim controller's Dashboard
        public IActionResult Dashboard()
        {
            return RedirectToAction("Dashboard", "Claim");
        }

        // REMOVED DUPLICATE Privacy() METHOD (Caused CS0111)
        /*
        public IActionResult Privacy()
        {
            return View();
        }
        */

        // REMOVED DUPLICATE Error() METHOD (Caused CS0111)
        /*
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        */

        private async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return null;
                }

                return await _userService.GetUserByIdAsync(userId);
            }
            // FIX for CS0168: Removed the unused exception variable 'e' or 'ex'
            catch (Exception)
            {
                return null;
            }
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                throw new InvalidOperationException("User ID claim is missing or invalid.");
            }
            return userId;
        }
    }
}