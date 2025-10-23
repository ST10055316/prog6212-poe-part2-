using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IO;
using Microsoft.Extensions.Logging;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using ClaimModel = ContractMonthlyClaimSystem.Models.Claim;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;
        private readonly ILogger<ClaimController> _logger;

        public ClaimController(IClaimService claimService, IUserService userService,
            ILogger<ClaimController> logger)
        {
            _claimService = claimService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Claims/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var dashboardData = await _claimService.GetDashboardDataAsync(currentUser);
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return View(new DashboardViewModel());
            }
        }

        // GET: Claims/Index
        public async Task<IActionResult> Index(ClaimSearchViewModel searchModel)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var searchResult = await _claimService.SearchClaimsAsync(searchModel, currentUser);

                // FIX: Properly map the claims to view models
                var viewModel = searchResult.Claims;
                ViewBag.TotalCount = searchResult.TotalCount;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading claims index");
                TempData["ErrorMessage"] = "An error occurred while loading claims.";
                return View(new List<ClaimViewModel>());
            }
        }

        // GET: Claims/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var claim = await _claimService.GetClaimByIdAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = MapClaimToViewModel(claim, currentUser);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading claim details for ID: {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading claim details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Claims/Pending - Shows claims awaiting current user's approval
        [Authorize]
        public async Task<IActionResult> Pending()
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var pendingClaims = await _claimService.GetClaimsAwaitingMyApprovalAsync(currentUser);

                ViewBag.UserRole = currentUser.Role.ToString();
                ViewBag.PendingCount = pendingClaims.Count;

                return View(pendingClaims);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending claims");
                TempData["ErrorMessage"] = "An error occurred while loading pending claims.";
                return View(new List<ClaimViewModel>());
            }
        }

        // GET: Claims/Create
        [Authorize(Roles = "Lecturer")]
        public IActionResult Create()
        {
            try
            {
                var viewModel = new SubmitClaimViewModel
                {
                    ClaimPeriod = DateTime.Now.Date,
                    HourlyRate = 0
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create claim form");
                TempData["ErrorMessage"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Claims/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Create(SubmitClaimViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                // Validate file if uploaded
                if (model.SupportingDocument != null)
                {
                    var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".jpg", ".png" };
                    var extension = Path.GetExtension(model.SupportingDocument.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("SupportingDocument",
                            "Invalid file type. Allowed types: PDF, DOCX, XLSX, JPG, PNG");
                        return View(model);
                    }

                    if (model.SupportingDocument.Length > 5 * 1024 * 1024) // 5MB
                    {
                        ModelState.AddModelError("SupportingDocument", "File size must not exceed 5MB");
                        return View(model);
                    }
                }

                // Create claim model from view model
                var claim = new ClaimModel
                {
                    LecturerId = currentUser.UserId,
                    HoursWorked = model.HoursWorked,
                    HourlyRate = model.HourlyRate,
                    ClaimPeriod = model.ClaimPeriod,
                    Description = model.Description,
                    Comments = model.Comments,
                    SubmissionDate = DateTime.Now,
                    Status = ClaimStatus.Submitted
                };

                var claimId = await _claimService.SubmitClaimAsync(claim, model.SupportingDocument);

                if (claimId > 0)
                {
                    _logger.LogInformation("Claim created successfully by user {UserId}", currentUser.UserId);
                    TempData["SuccessMessage"] = "Claim submitted successfully!";
                    return RedirectToAction(nameof(Details), new { id = claimId });
                }
                else
                {
                    ModelState.AddModelError("", "Error creating claim. Please try again.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim");
                ModelState.AddModelError("", $"Error creating claim: {ex.Message}");
                return View(model);
            }
        }

        // GET: Claims/Edit/5
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var claim = await _claimService.GetClaimByIdAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Only allow editing own claims and only if status is Submitted
                if (claim.LecturerId != currentUser.UserId)
                {
                    TempData["ErrorMessage"] = "You can only edit your own claims.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (claim.Status != ClaimStatus.Submitted)
                {
                    TempData["ErrorMessage"] = "Only claims with 'Submitted' status can be edited.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var viewModel = new SubmitClaimViewModel
                {
                    ClaimId = claim.ClaimId,
                    HoursWorked = claim.HoursWorked,
                    HourlyRate = claim.HourlyRate,
                    ClaimPeriod = claim.ClaimPeriod,
                    Description = claim.Description,
                    Comments = claim.Comments,
                    ExistingFileName = claim.FileName
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for claim ID: {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the claim.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Claims/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Edit(int id, SubmitClaimViewModel model)
        {
            try
            {
                if (id != model.ClaimId)
                {
                    return BadRequest();
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var claim = await _claimService.GetClaimByIdAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify ownership
                if (claim.LecturerId != currentUser.UserId)
                {
                    TempData["ErrorMessage"] = "You can only edit your own claims.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Validate file if uploaded
                if (model.SupportingDocument != null)
                {
                    var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".jpg", ".png" };
                    var extension = Path.GetExtension(model.SupportingDocument.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("SupportingDocument",
                            "Invalid file type. Allowed types: PDF, DOCX, XLSX, JPG, PNG");
                        return View(model);
                    }

                    if (model.SupportingDocument.Length > 5 * 1024 * 1024) // 5MB
                    {
                        ModelState.AddModelError("SupportingDocument", "File size must not exceed 5MB");
                        return View(model);
                    }
                }

                // Update claim with new values
                claim.HoursWorked = model.HoursWorked;
                claim.HourlyRate = model.HourlyRate;
                claim.ClaimPeriod = model.ClaimPeriod;
                claim.Description = model.Description;
                claim.Comments = model.Comments;

                var success = await _claimService.UpdateClaimAsync(claim, model.SupportingDocument);

                if (success)
                {
                    _logger.LogInformation("Claim {ClaimId} updated by user {UserId}", id, currentUser.UserId);
                    TempData["SuccessMessage"] = "Claim updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    ModelState.AddModelError("", "Error updating claim. Please try again.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim ID: {ClaimId}", id);
                ModelState.AddModelError("", $"Error updating claim: {ex.Message}");
                return View(model);
            }
        }

        // GET: Claims/Approve/5
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var claim = await _claimService.GetClaimByIdAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Pending));
                }

                // Check if user can approve this claim
                if (!CanUserApproveClaim(claim, currentUser))
                {
                    TempData["ErrorMessage"] = "You cannot approve this claim at this time.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var viewModel = new ApproveRejectViewModel
                {
                    ClaimId = id,
                    Action = "Approve",
                    Claim = MapClaimToViewModel(claim, currentUser)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading approve form for claim ID: {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the approval form.";
                return RedirectToAction(nameof(Pending));
            }
        }

        // POST: Claims/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Approve(ApproveRejectViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var claim = await _claimService.GetClaimByIdAsync(model.ClaimId);
                    var user = await GetCurrentUserAsync();
                    if (claim != null && user != null)
                    {
                        model.Claim = MapClaimToViewModel(claim, user);
                    }
                    return View(model);
                }

                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var claimToApprove = await _claimService.GetClaimByIdAsync(model.ClaimId);
                if (claimToApprove == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Pending));
                }

                // Check if user can approve this claim
                if (!CanUserApproveClaim(claimToApprove, currentUser))
                {
                    TempData["ErrorMessage"] = "You cannot approve this claim at this time.";
                    return RedirectToAction(nameof(Details), new { id = model.ClaimId });
                }

                var success = await _claimService.ApproveClaimAsync(model.ClaimId,
                    currentUser.UserId, model.Comments ?? string.Empty);

                if (success)
                {
                    var roleName = currentUser.Role == UserRole.ProgrammeCoordinator ?
                        "Programme Coordinator" : "Academic Manager";

                    _logger.LogInformation("Claim {ClaimId} approved by {Role} {UserId}",
                        model.ClaimId, roleName, currentUser.UserId);

                    TempData["SuccessMessage"] = $"Claim approved successfully! " +
                        $"{(currentUser.Role == UserRole.ProgrammeCoordinator ? "Awaiting Academic Manager final approval." : "Claim fully approved.")}";

                    return RedirectToAction(nameof(Details), new { id = model.ClaimId });
                }
                else
                {
                    ModelState.AddModelError("", "Error approving claim. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving claim ID: {ClaimId}", model.ClaimId);
                ModelState.AddModelError("", $"Error approving claim: {ex.Message}");
            }

            // Reload claim data for display if there was an error
            var reloadClaim = await _claimService.GetClaimByIdAsync(model.ClaimId);
            var reloadUser = await GetCurrentUserAsync();
            if (reloadClaim != null && reloadUser != null)
            {
                model.Claim = MapClaimToViewModel(reloadClaim, reloadUser);
            }
            return View(model);
        }

        // GET: Claims/Reject/5
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var claim = await _claimService.GetClaimByIdAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Pending));
                }

                // Check if user can reject this claim
                if (!CanUserApproveClaim(claim, currentUser))
                {
                    TempData["ErrorMessage"] = "You cannot reject this claim at this time.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var viewModel = new ApproveRejectViewModel
                {
                    ClaimId = id,
                    Action = "Reject",
                    Claim = MapClaimToViewModel(claim, currentUser)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reject form for claim ID: {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the rejection form.";
                return RedirectToAction(nameof(Pending));
            }
        }

        // POST: Claims/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Reject(ApproveRejectViewModel model)
        {
            try
            {
                // Comments are required for rejection
                if (string.IsNullOrWhiteSpace(model.Comments))
                {
                    ModelState.AddModelError("Comments", "Comments are required when rejecting a claim.");
                }

                if (!ModelState.IsValid)
                {
                    var claim = await _claimService.GetClaimByIdAsync(model.ClaimId);
                    var user = await GetCurrentUserAsync();
                    if (claim != null && user != null)
                    {
                        model.Claim = MapClaimToViewModel(claim, user);
                    }
                    return View(model);
                }

                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var claimToReject = await _claimService.GetClaimByIdAsync(model.ClaimId);
                if (claimToReject == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Pending));
                }

                // Check if user can reject this claim
                if (!CanUserApproveClaim(claimToReject, currentUser))
                {
                    TempData["ErrorMessage"] = "You cannot reject this claim at this time.";
                    return RedirectToAction(nameof(Details), new { id = model.ClaimId });
                }

                var success = await _claimService.RejectClaimAsync(model.ClaimId,
                    currentUser.UserId, model.Comments.Trim());

                if (success)
                {
                    var roleName = currentUser.Role == UserRole.ProgrammeCoordinator ?
                        "Programme Coordinator" : "Academic Manager";

                    _logger.LogInformation("Claim {ClaimId} rejected by {Role} {UserId}",
                        model.ClaimId, roleName, currentUser.UserId);

                    TempData["SuccessMessage"] = "Claim rejected successfully!";
                    return RedirectToAction(nameof(Details), new { id = model.ClaimId });
                }
                else
                {
                    ModelState.AddModelError("", "Error rejecting claim. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting claim ID: {ClaimId}", model.ClaimId);
                ModelState.AddModelError("", $"Error rejecting claim: {ex.Message}");
            }

            // Reload claim data for display if there was an error
            var reloadClaim = await _claimService.GetClaimByIdAsync(model.ClaimId);
            var reloadUser = await GetCurrentUserAsync();
            if (reloadClaim != null && reloadUser != null)
            {
                model.Claim = MapClaimToViewModel(reloadClaim, reloadUser);
            }
            return View(model);
        }

        // GET: Claims/DownloadDocument/5
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized();

                var claim = await _claimService.GetClaimByIdAsync(id);
                if (claim == null)
                {
                    TempData["ErrorMessage"] = "Claim not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if user has permission to view this document
                if (claim.LecturerId != currentUser.UserId &&
                    currentUser.Role != UserRole.ProgrammeCoordinator &&
                    currentUser.Role != UserRole.AcademicManager)
                {
                    TempData["ErrorMessage"] = "You do not have permission to view this document.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (claim.FileData == null || claim.FileData.Length == 0)
                {
                    TempData["ErrorMessage"] = "No document found for this claim.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return File(claim.FileData, claim.FileContentType ?? "application/octet-stream",
                    claim.FileName ?? $"claim_{id}_document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document for claim ID: {ClaimId}", id);
                TempData["ErrorMessage"] = "An error occurred while downloading the document.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // Helper method to check if user can approve/reject a claim
        private bool CanUserApproveClaim(ClaimModel claim, User user)
        {
            // Can't approve own claims
            if (claim.LecturerId == user.UserId)
                return false;

            // Check if user has already approved/rejected this claim
            var existingApproval = claim.Approvals.FirstOrDefault(a => a.ApproverId == user.UserId);
            if (existingApproval != null)
                return false;

            // Programme Coordinators can only approve Submitted claims
            if (user.Role == UserRole.ProgrammeCoordinator)
            {
                return claim.Status == ClaimStatus.Submitted;
            }

            // Academic Managers can only approve UnderReview claims that have Programme Coordinator approval
            if (user.Role == UserRole.AcademicManager)
            {
                return claim.Status == ClaimStatus.UnderReview &&
                       claim.Approvals.Any(a =>
                           a.Approver.Role == UserRole.ProgrammeCoordinator &&
                           a.Status == ApprovalStatus.Approved);
            }

            return false;
        }

        // Map ClaimModel to ClaimViewModel
        private ClaimViewModel MapClaimToViewModel(ClaimModel claim, User currentUser)
        {
            var hasPcApproval = claim.Approvals.Any(a =>
                a.Approver.Role == UserRole.ProgrammeCoordinator &&
                a.Status == ApprovalStatus.Approved);

            var viewModel = new ClaimViewModel
            {
                ClaimId = claim.ClaimId,
                LecturerName = claim.Lecturer?.Name ?? "Unknown",
                HoursWorked = claim.HoursWorked,
                HourlyRate = claim.HourlyRate,
                TotalAmount = claim.HoursWorked * claim.HourlyRate,
                Status = claim.Status.ToString(),
                SubmissionDate = claim.SubmissionDate,
                ClaimPeriod = claim.ClaimPeriod,
                Description = claim.Description,
                Comments = claim.Comments,
                FileName = claim.FileName,
                FileContentType = claim.FileContentType,
                FileSize = claim.FileSize,
                HasSupportingDocument = !string.IsNullOrEmpty(claim.FileName),
                CanApprove = CanUserApproveClaim(claim, currentUser),
                CanReject = CanUserApproveClaim(claim, currentUser),
                Department = claim.Lecturer?.Department ?? "General",
                HasApprovalFromProgrammeCoordinator = hasPcApproval,
                Approvals = claim.Approvals.Select(a => new ApprovalViewModel
                {
                    ApproverName = a.Approver?.Name ?? "Unknown",
                    ApproverRole = a.Approver?.Role.ToString() ?? "Unknown",
                    Status = a.Status.ToString(),
                    ApprovalDate = a.ApprovalDate,
                    Comments = a.Comments
                }).ToList()
            };

            return viewModel;
        }

        // Helper to get current user
        private async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true) return null;

                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return null;

                return await _userService.GetUserByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }
    }
}