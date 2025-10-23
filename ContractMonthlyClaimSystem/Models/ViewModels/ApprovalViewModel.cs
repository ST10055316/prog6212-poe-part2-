using System;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ApprovalViewModel
    {
        public int ApprovalId { get; set; }

        [Display(Name = "Approver Name")]
        public string ApproverName { get; set; } = string.Empty;

        [Display(Name = "Approver Role")]
        public string? ApproverRole { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Approval Date")]
        [DataType(DataType.DateTime)]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Comments")]
        public string? Comments { get; set; }
    }
}