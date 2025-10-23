using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Claim = ContractMonthlyClaimSystem.Models.Claim;

namespace ContractMonthlyClaimSystem.Services
{
    public interface IClaimService
    {
        // Dashboard methods
        Task<DashboardViewModel> GetDashboardDataAsync(User currentUser);
        Task<List<ClaimViewModel>> GetClaimsAwaitingMyApprovalAsync(User currentUser);

        // Claim CRUD operations
        Task<int> SubmitClaimAsync(Claim claim, IFormFile? supportingDocument = null);
        Task<int> SubmitClaimAsync(Claim claim);
        Task<Claim?> GetClaimByIdAsync(int claimId);
        Task<bool> UpdateClaimAsync(Claim claim, IFormFile? supportingDocument = null);

        // File operations
        Task<(byte[]? fileData, string? contentType, string? fileName)> DownloadSupportingDocumentAsync(int claimId);

        // Claim retrieval methods
        Task<List<Claim>> GetClaimsByLecturerAsync(int lecturerId);
        Task<List<Claim>> GetPendingClaimsAsync(UserRole approverRole);
        Task<List<Claim>> GetAllClaimsAsync();
        Task<List<ClaimViewModel>> GetClaimsByUserAsync(int userId);
        Task<List<ClaimViewModel>> GetSubmittedClaimsAsync();
        Task<List<ClaimViewModel>> GetClaimsForProgrammeCoordinatorAsync();
        Task<List<ClaimViewModel>> GetClaimsForAcademicManagerAsync();

        // Approval workflow methods
        Task<bool> ApproveClaimAsync(int claimId, int approverId, string comments = "");
        Task<bool> RejectClaimAsync(int claimId, int approverId, string comments);

        // Search and filtering
        Task<(List<ClaimViewModel> Claims, int TotalCount)> SearchClaimsAsync(ClaimSearchViewModel searchModel, User currentUser);

        // Reporting methods
        Task<List<ClaimSummaryDto>> GetClaimsSummaryAsync(int userId, UserRole userRole);
        Task<object> GetReportsDataAsync();
        Task<object> GetMonthlyReportAsync(int year, int month);
        Task<object> GetLecturerReportAsync(int lecturerId, int year);
    }
}