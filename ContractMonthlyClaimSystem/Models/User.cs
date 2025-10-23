using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    // The missing UserStatus enum definition (must be in the same or an accessible namespace)
    public enum UserStatus
    {
        Pending,
        Active,
        Inactive,
        Suspended
    }

    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        [StringLength(100)]
        public string Department { get; set; } = "General";

        // FIX: Added the missing Status property as required by AccountController.cs
        [Required]
        public UserStatus Status { get; set; } = UserStatus.Pending;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    }

    // The rest of the models/enums in this file (e.g., UserRole, Claim, etc.)
}