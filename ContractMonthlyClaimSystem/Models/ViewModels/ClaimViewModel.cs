using System;
using System.Collections.Generic;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ClaimViewModel
    {
        public int ClaimId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public DateTime ClaimPeriod { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public string? FileName { get; set; }
        public string? FileContentType { get; set; }
        public long? FileSize { get; set; }
        public bool HasSupportingDocument { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
       
        public bool HasApprovalFromProgrammeCoordinator { get; set; } // Add this property
        public List<ApprovalViewModel> Approvals { get; set; } = new List<ApprovalViewModel>();
    }
}
   