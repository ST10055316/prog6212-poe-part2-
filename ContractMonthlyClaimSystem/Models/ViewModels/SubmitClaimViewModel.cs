using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class SubmitClaimViewModel
    {
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "Hours worked is required")]
        [Range(0.1, 744, ErrorMessage = "Hours worked must be between 0.1 and 744")]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(0.01, 10000, ErrorMessage = "Hourly rate must be between 0.01 and 10000")]
        [Display(Name = "Hourly Rate")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [Required(ErrorMessage = "Claim period is required")]
        [Display(Name = "Claim Period")]
        [DataType(DataType.Date)]
        public DateTime ClaimPeriod { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters")]
        [Display(Name = "Additional Comments")]
        public string? Comments { get; set; }

        [Display(Name = "Supporting Document")]
        public IFormFile? SupportingDocument { get; set; }

        // For displaying existing file name when editing
        [Display(Name = "Current Document")]
        public string? ExistingFileName { get; set; }

        // Computed property
        public decimal TotalAmount => HoursWorked * HourlyRate;
    }
}