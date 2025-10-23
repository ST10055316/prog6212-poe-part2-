using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ApproveRejectViewModel
    {
        [Required]
        public int ClaimId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // "Approve" or "Reject"

        [StringLength(500)]
        [Display(Name = "Comments")]
        [DataType(DataType.MultilineText)]
        public string Comments { get; set; } = string.Empty;

        // Display properties
        public ClaimViewModel Claim { get; set; } = new ClaimViewModel();
    }
}