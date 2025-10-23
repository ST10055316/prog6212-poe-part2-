using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ContractMonthlyClaimSystem.Services;
using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize(Roles = "AcademicManager")]
    public class ReportsController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;

        public ReportsController(IClaimService claimService, IUserService userService)
        {
            _claimService = claimService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var reports = await _claimService.GetReportsDataAsync();
            return View(reports);
        }

        public async Task<IActionResult> MonthlyReport(int year = 0, int month = 0)
        {
            if (year == 0) year = DateTime.Now.Year;
            if (month == 0) month = DateTime.Now.Month;

            var report = await _claimService.GetMonthlyReportAsync(year, month);
            ViewBag.Year = year;
            ViewBag.Month = month;

            return View(report);
        }

        public async Task<IActionResult> LecturerReport(int lecturerId, int year = 0)
        {
            if (year == 0) year = DateTime.Now.Year;

            var lecturer = await _userService.GetUserByIdAsync(lecturerId);
            if (lecturer == null || lecturer.Role != UserRole.Lecturer)
            {
                return NotFound();
            }

            var report = await _claimService.GetLecturerReportAsync(lecturerId, year);
            ViewBag.Lecturer = lecturer;
            ViewBag.Year = year;

            return View(report);
        }
    }
}

