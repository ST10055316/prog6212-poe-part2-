using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Role")]
        public string Role { get; set; }

        [StringLength(100)]
        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Member Since")]
        public DateTime CreatedDate { get; set; }

        // Statistics
        public int TotalClaims { get; set; }
        public decimal TotalEarnings { get; set; }
        public int PendingApprovals { get; set; }
    }
}
