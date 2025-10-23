// ContractMonthlyClaimSystem.Models/ClaimApproval.cs
using System;
using System.ComponentModel.DataAnnotations;
// ... other usings

namespace ContractMonthlyClaimSystem.Models
{
    public class ClaimApproval
    {
        [Key]
        public int ApprovalId { get; set; }

        public int ClaimId { get; set; }
        public int ApproverId { get; set; }

        // 👇 CHANGE: Make ApprovalDate nullable
        public DateTime? ApprovalDate { get; set; }

        public ApprovalStatus Status { get; set; }
        public string? Comments { get; set; } // Nullable

        // Navigation properties
        public virtual Claim Claim { get; set; } = null!;
        public virtual User Approver { get; set; } = null!;
    }
}