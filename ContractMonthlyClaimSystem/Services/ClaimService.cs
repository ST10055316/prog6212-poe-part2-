using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Claim = ContractMonthlyClaimSystem.Models.Claim;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ContractMonthlyClaimSystem.Services
{
    public class ClaimService : IClaimService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClaimService> _logger;

        public ClaimService(ApplicationDbContext context, ILogger<ClaimService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> SubmitClaimAsync(Claim claim, IFormFile? supportingDocument = null)
        {
            if (claim.LecturerId <= 0)
                throw new ArgumentException("LecturerId is required");

            if (claim.ClaimPeriod == default)
                throw new ArgumentException("ClaimPeriod must be selected");

            if (claim.HoursWorked <= 0 || claim.HoursWorked > 744)
                throw new ArgumentException("Hours worked must be between 0.1 and 744");

            if (string.IsNullOrWhiteSpace(claim.Description))
                throw new ArgumentException("Description is required");

            // Check for duplicate claims for same lecturer and month
            var existingClaim = await _context.Claims
                .Where(c => c.LecturerId == claim.LecturerId
                         && c.ClaimPeriod.Year == claim.ClaimPeriod.Year
                         && c.ClaimPeriod.Month == claim.ClaimPeriod.Month
                         && c.Status != ClaimStatus.Rejected
                         && c.Status != ClaimStatus.Draft)
                .FirstOrDefaultAsync();

            if (existingClaim != null)
                throw new InvalidOperationException("A claim already exists for this lecturer and month.");

            // Handle file upload
            if (supportingDocument != null && supportingDocument.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await supportingDocument.CopyToAsync(memoryStream);

                claim.FileData = memoryStream.ToArray();
                claim.FileName = Path.GetFileName(supportingDocument.FileName);
                claim.FileContentType = supportingDocument.ContentType;
                claim.FileSize = supportingDocument.Length;
            }

            claim.SubmissionDate = DateTime.UtcNow;

            // If not draft, set status to Submitted for approval workflow
            if (claim.Status != ClaimStatus.Draft)
            {
                claim.Status = ClaimStatus.Submitted;
            }

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Claim {ClaimId} submitted by lecturer {LecturerId}", claim.ClaimId, claim.LecturerId);
            return claim.ClaimId;
        }

        public async Task<int> SubmitClaimAsync(Claim claim)
        {
            return await SubmitClaimAsync(claim, null);
        }

        public async Task<bool> ApproveClaimAsync(int claimId, int approverId, string comments = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Approvals)
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.ClaimId == claimId);

                if (claim == null) return false;

                var approver = await _context.Users.FindAsync(approverId);
                if (approver == null) return false;

                // Check if user has already approved/rejected this claim
                var existingApproval = claim.Approvals.FirstOrDefault(a => a.ApproverId == approverId);
                if (existingApproval != null) return false;

                // Create approval record
                var approval = new Approval
                {
                    ClaimId = claimId,
                    ApproverId = approverId,
                    Status = ApprovalStatus.Approved,
                    ApprovalDate = DateTime.UtcNow,
                    Comments = comments
                };

                _context.Approvals.Add(approval);

                // Update claim status based on workflow
                if (approver.Role == UserRole.ProgrammeCoordinator)
                {
                    claim.Status = ClaimStatus.UnderReview;
                }
                else if (approver.Role == UserRole.AcademicManager)
                {
                    claim.Status = ClaimStatus.Approved;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving claim ID: {ClaimId}", claimId);
                return false;
            }
        }

        public async Task<bool> RejectClaimAsync(int claimId, int approverId, string comments)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var claim = await _context.Claims
                    .Include(c => c.Approvals)
                    .FirstOrDefaultAsync(c => c.ClaimId == claimId);

                if (claim == null) return false;

                var approver = await _context.Users.FindAsync(approverId);
                if (approver == null) return false;

                // Check if user has already approved/rejected this claim
                var existingApproval = claim.Approvals.FirstOrDefault(a => a.ApproverId == approverId);
                if (existingApproval != null) return false;

                // Create rejection record
                var approval = new Approval
                {
                    ClaimId = claimId,
                    ApproverId = approverId,
                    Status = ApprovalStatus.Rejected,
                    ApprovalDate = DateTime.UtcNow,
                    Comments = comments
                };

                _context.Approvals.Add(approval);

                // Update claim status to rejected
                claim.Status = ClaimStatus.Rejected;
                claim.Comments = comments;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Claim {ClaimId} rejected by {ApproverRole} {ApproverId}", claimId, approver.Role, approverId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting claim ID: {ClaimId}", claimId);
                return false;
            }
        }

        // Get claims awaiting Programme Coordinator approval (Submitted status)
        public async Task<List<ClaimViewModel>> GetClaimsForProgrammeCoordinatorAsync()
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .Where(c => c.Status == ClaimStatus.Submitted)
                .Select(c => new ClaimViewModel
                {
                    ClaimId = c.ClaimId,
                    LecturerName = c.Lecturer.Name,
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    Status = c.Status.ToString(),
                    SubmissionDate = c.SubmissionDate,
                    ClaimPeriod = c.ClaimPeriod,
                    Description = c.Description,
                    Department = c.Lecturer.Department,
                    HasSupportingDocument = !string.IsNullOrEmpty(c.FileName),
                    HasApprovalFromProgrammeCoordinator = c.Approvals.Any(a =>
                        a.Approver.Role == UserRole.ProgrammeCoordinator &&
                        a.Status == ApprovalStatus.Approved)
                })
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // Get claims awaiting Academic Manager approval (UnderReview status with Programme Coordinator approval)
        public async Task<List<ClaimViewModel>> GetClaimsForAcademicManagerAsync()
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .Where(c => c.Status == ClaimStatus.UnderReview &&
                           c.Approvals.Any(a =>
                               a.Approver.Role == UserRole.ProgrammeCoordinator &&
                               a.Status == ApprovalStatus.Approved))
                .Select(c => new ClaimViewModel
                {
                    ClaimId = c.ClaimId,
                    LecturerName = c.Lecturer.Name,
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    Status = c.Status.ToString(),
                    SubmissionDate = c.SubmissionDate,
                    ClaimPeriod = c.ClaimPeriod,
                    Description = c.Description,
                    Department = c.Lecturer.Department,
                    HasSupportingDocument = !string.IsNullOrEmpty(c.FileName),
                    HasApprovalFromProgrammeCoordinator = true
                })
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<List<ClaimViewModel>> GetClaimsAwaitingMyApprovalAsync(User currentUser)
        {
            if (currentUser.Role == UserRole.ProgrammeCoordinator)
            {
                return await GetClaimsForProgrammeCoordinatorAsync();
            }
            else if (currentUser.Role == UserRole.AcademicManager)
            {
                return await GetClaimsForAcademicManagerAsync();
            }

            return new List<ClaimViewModel>();
        }

        public async Task<Claim?> GetClaimByIdAsync(int claimId)
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(User currentUser)
        {
            var query = _context.Claims.AsQueryable();

            // Filter by lecturer if user is a lecturer
            if (currentUser.Role == UserRole.Lecturer)
            {
                query = query.Where(c => c.LecturerId == currentUser.UserId);
            }

            var totalClaims = await query.CountAsync();
            var approvedClaims = await query.CountAsync(c => c.Status == ClaimStatus.Approved);
            var pendingClaims = await query.CountAsync(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview);
            var rejectedClaims = await query.CountAsync(c => c.Status == ClaimStatus.Rejected);

            // Calculate amounts
            var totalAmount = await query.SumAsync(c => c.HoursWorked * c.HourlyRate);
            var approvedAmount = await query.Where(c => c.Status == ClaimStatus.Approved)
                                          .SumAsync(c => c.HoursWorked * c.HourlyRate);

            // Get recent claims
            var recentClaims = await query
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                .OrderByDescending(c => c.SubmissionDate)
                .Take(10)
                .Select(c => new ClaimViewModel
                {
                    ClaimId = c.ClaimId,
                    LecturerName = c.Lecturer.Name,
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    Status = c.Status.ToString(),
                    SubmissionDate = c.SubmissionDate,
                    Description = c.Description,
                    FileName = c.FileName,
                    HasSupportingDocument = !string.IsNullOrEmpty(c.FileName),
                    HasApprovalFromProgrammeCoordinator = c.Approvals.Any(a =>
                        a.Approver.Role == UserRole.ProgrammeCoordinator &&
                        a.Status == ApprovalStatus.Approved),
                    Department = c.Lecturer.Department
                })
                .ToListAsync();

            // Get claims awaiting approval for coordinators/managers
            var claimsAwaitingMyApproval = new List<ClaimViewModel>();
            if (currentUser.Role == UserRole.ProgrammeCoordinator || currentUser.Role == UserRole.AcademicManager)
            {
                var claimsQuery = _context.Claims
                    .Include(c => c.Lecturer)
                    .Include(c => c.Approvals)
                        .ThenInclude(a => a.Approver)
                    .Where(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview)
                    .AsQueryable();

                // Filter based on role and approval workflow
                if (currentUser.Role == UserRole.ProgrammeCoordinator)
                {
                    claimsQuery = claimsQuery.Where(c => c.Status == ClaimStatus.Submitted &&
                                                        !c.Approvals.Any(a => a.ApproverId == currentUser.UserId));
                }
                else if (currentUser.Role == UserRole.AcademicManager)
                {
                    claimsQuery = claimsQuery.Where(c => c.Status == ClaimStatus.UnderReview &&
                                                        c.Approvals.Any(a => a.Approver.Role == UserRole.ProgrammeCoordinator &&
                                                                           a.Status == ApprovalStatus.Approved) &&
                                                        !c.Approvals.Any(a => a.ApproverId == currentUser.UserId));
                }

                claimsAwaitingMyApproval = await claimsQuery
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(10)
                    .Select(c => new ClaimViewModel
                    {
                        ClaimId = c.ClaimId,
                        LecturerName = c.Lecturer.Name,
                        HoursWorked = c.HoursWorked,
                        HourlyRate = c.HourlyRate,
                        TotalAmount = c.HoursWorked * c.HourlyRate,
                        Status = c.Status.ToString(),
                        SubmissionDate = c.SubmissionDate,
                        Description = c.Description,
                        Department = c.Lecturer.Department,
                        HasSupportingDocument = !string.IsNullOrEmpty(c.FileName),
                        HasApprovalFromProgrammeCoordinator = c.Approvals.Any(a =>
                            a.Approver.Role == UserRole.ProgrammeCoordinator &&
                            a.Status == ApprovalStatus.Approved),
                        CanApprove = true
                    })
                    .ToListAsync();
            }

            return new DashboardViewModel
            {
                TotalClaims = totalClaims,
                ApprovedClaims = approvedClaims,
                PendingClaims = pendingClaims,
                RejectedClaims = rejectedClaims,
                TotalAmount = totalAmount,
                ApprovedAmount = approvedAmount,
                RecentClaims = recentClaims,
                ClaimsAwaitingMyApproval = claimsAwaitingMyApproval
            };
        }

        public async Task<(byte[]? fileData, string? contentType, string? fileName)> DownloadSupportingDocumentAsync(int claimId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null || claim.FileData == null)
                return (null, null, null);

            return (claim.FileData, claim.FileContentType, claim.FileName);
        }

        public async Task<List<Claim>> GetClaimsByLecturerAsync(int lecturerId)
        {
            return await _context.Claims
                .Where(c => c.LecturerId == lecturerId)
                .Include(c => c.Lecturer)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<List<Claim>> GetPendingClaimsAsync(UserRole approverRole)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .AsQueryable();

            if (approverRole == UserRole.ProgrammeCoordinator)
            {
                query = query.Where(c => c.Status == ClaimStatus.Submitted);
            }
            else if (approverRole == UserRole.AcademicManager)
            {
                query = query.Where(c => c.Status == ClaimStatus.UnderReview);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Claim>> GetAllClaimsAsync()
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateClaimAsync(Claim claim, IFormFile? supportingDocument = null)
        {
            var existing = await _context.Claims.FindAsync(claim.ClaimId);
            if (existing == null) return false;

            existing.Description = claim.Description;
            existing.HoursWorked = claim.HoursWorked;
            existing.HourlyRate = claim.HourlyRate;
            existing.ClaimPeriod = claim.ClaimPeriod;
            existing.Status = claim.Status;
            existing.Comments = claim.Comments;

            if (supportingDocument != null && supportingDocument.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await supportingDocument.CopyToAsync(memoryStream);

                existing.FileData = memoryStream.ToArray();
                existing.FileName = Path.GetFileName(supportingDocument.FileName);
                existing.FileContentType = supportingDocument.ContentType;
                existing.FileSize = supportingDocument.Length;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(List<ClaimViewModel> Claims, int TotalCount)> SearchClaimsAsync(ClaimSearchViewModel searchModel, User currentUser)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                .AsQueryable();

            // Role-based filtering
            if (currentUser.Role == UserRole.Lecturer)
            {
                query = query.Where(c => c.LecturerId == currentUser.UserId);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(searchModel.Status))
            {
                if (Enum.TryParse<ClaimStatus>(searchModel.Status, out var status))
                {
                    query = query.Where(c => c.Status == status);
                }
            }

            if (searchModel.FromDate.HasValue)
            {
                query = query.Where(c => c.ClaimPeriod >= searchModel.FromDate.Value);
            }

            if (searchModel.ToDate.HasValue)
            {
                query = query.Where(c => c.ClaimPeriod <= searchModel.ToDate.Value);
            }

            if (searchModel.LecturerId.HasValue)
            {
                query = query.Where(c => c.LecturerId == searchModel.LecturerId.Value);
            }

            if (!string.IsNullOrEmpty(searchModel.SearchText))
            {
                query = query.Where(c =>
                    c.Description.Contains(searchModel.SearchText) ||
                    c.Lecturer.Name.Contains(searchModel.SearchText));
            }

            var totalCount = await query.CountAsync();

            var claims = await query
                .OrderByDescending(c => c.SubmissionDate)
                .Skip((searchModel.Page - 1) * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .Select(c => new ClaimViewModel
                {
                    ClaimId = c.ClaimId,
                    LecturerName = c.Lecturer.Name,
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    Status = c.Status.ToString(),
                    SubmissionDate = c.SubmissionDate,
                    ClaimPeriod = c.ClaimPeriod,
                    Description = c.Description,
                    Department = c.Lecturer.Department,
                    HasSupportingDocument = !string.IsNullOrEmpty(c.FileName),
                    HasApprovalFromProgrammeCoordinator = c.Approvals.Any(a =>
                        a.Approver.Role == UserRole.ProgrammeCoordinator &&
                        a.Status == ApprovalStatus.Approved)
                })
                .ToListAsync();

            return (claims, totalCount);
        }

        public async Task<List<ClaimSummaryDto>> GetClaimsSummaryAsync(int userId, UserRole userRole)
        {
            var query = _context.Claims.AsQueryable();

            if (userRole == UserRole.Lecturer)
            {
                query = query.Where(c => c.LecturerId == userId);
            }

            return await query
                .GroupBy(c => new { c.ClaimPeriod.Year, c.ClaimPeriod.Month })
                .Select(g => new ClaimSummaryDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalAmount = g.Sum(c => c.HoursWorked * c.HourlyRate),
                    ClaimCount = g.Count()
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();
        }

        public async Task<object> GetReportsDataAsync()
        {
            var totalClaims = await _context.Claims.CountAsync();
            var approvedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Approved);
            var pendingClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview);
            var rejectedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Rejected);

            var totalAmount = await _context.Claims
                .Where(c => c.Status == ClaimStatus.Approved)
                .SumAsync(c => c.HoursWorked * c.HourlyRate);

            return new
            {
                TotalClaims = totalClaims,
                ApprovedClaims = approvedClaims,
                PendingClaims = pendingClaims,
                RejectedClaims = rejectedClaims,
                TotalAmount = totalAmount,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<object> GetMonthlyReportAsync(int year, int month)
        {
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.ClaimPeriod.Year == year && c.ClaimPeriod.Month == month)
                .Select(c => new
                {
                    c.ClaimId,
                    LecturerName = c.Lecturer.Name,
                    c.HoursWorked,
                    c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    c.Status,
                    c.SubmissionDate
                })
                .ToListAsync();

            return new
            {
                Year = year,
                Month = month,
                TotalClaims = claims.Count,
                TotalAmount = claims.Sum(c => c.TotalAmount),
                Claims = claims,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<object> GetLecturerReportAsync(int lecturerId, int year)
        {
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.LecturerId == lecturerId && c.ClaimPeriod.Year == year)
                .Select(c => new
                {
                    c.ClaimId,
                    c.ClaimPeriod,
                    c.HoursWorked,
                    c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    c.Status,
                    c.SubmissionDate
                })
                .ToListAsync();

            var lecturer = await _context.Users.FindAsync(lecturerId);

            return new
            {
                LecturerId = lecturerId,
                LecturerName = lecturer?.Name,
                Year = year,
                TotalClaims = claims.Count,
                TotalAmount = claims.Sum(c => c.TotalAmount),
                ApprovedAmount = claims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount),
                Claims = claims,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<List<ClaimViewModel>> GetSubmittedClaimsAsync()
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Submitted)
                .Select(c => new ClaimViewModel
                {
                    ClaimId = c.ClaimId,
                    LecturerName = c.Lecturer.Name,
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    Status = c.Status.ToString(),
                    SubmissionDate = c.SubmissionDate,
                    ClaimPeriod = c.ClaimPeriod,
                    Description = c.Description,
                    Department = c.Lecturer.Department
                })
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<List<ClaimViewModel>> GetClaimsByUserAsync(int userId)
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.LecturerId == userId)
                .Select(c => new ClaimViewModel
                {
                    ClaimId = c.ClaimId,
                    LecturerName = c.Lecturer.Name,
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    Status = c.Status.ToString(),
                    SubmissionDate = c.SubmissionDate,
                    ClaimPeriod = c.ClaimPeriod,
                    Description = c.Description,
                    Department = c.Lecturer.Department,
                    HasSupportingDocument = !string.IsNullOrEmpty(c.FileName)
                })
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }
    }
}