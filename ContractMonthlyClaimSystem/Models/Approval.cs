using System;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class Approval
    {
        public int ApprovalId { get; set; }

        [Required]
        public int ClaimId { get; set; }
        public Claim Claim { get; set; } = null!;

        [Required]
        public int ApproverId { get; set; }
        public User Approver { get; set; } = null!;

        [Required]
        public ApprovalStatus Status { get; set; }

        public DateTime ApprovalDate { get; set; } = DateTime.Now;

        [StringLength(1000)]
        public string? Comments { get; set; }
    }

    
}