using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }

        [Required]
        public int LecturerId { get; set; }
        public User Lecturer { get; set; } = null!;

        [Required]
        [Range(0.1, 744)]
        public decimal HoursWorked { get; set; }

        [Required]
        [Range(1, 1000)]
        public decimal HourlyRate { get; set; }

        [Required]
        public DateTime ClaimPeriod { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public string? Comments { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        // File upload properties
        public byte[]? FileData { get; set; }
        public string? FileName { get; set; }
        public string? FileContentType { get; set; }
        public long? FileSize { get; set; }

        // Navigation properties
        public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
        public decimal TotalAmount => HoursWorked * HourlyRate;
    }

    public enum ClaimStatus
    {
        Draft,
        Submitted,
        UnderReview,    // After Programme Coordinator approval
        Approved,       // After Academic Manager approval
        Rejected
    }
}