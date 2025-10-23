using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
// In Models/ViewModels/DashboardViewModel.cs
using System.Collections.Generic;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int PendingApprovals { get; set; } // Add this
        public decimal TotalAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public List<ClaimViewModel> RecentClaims { get; set; } = new List<ClaimViewModel>();
        public List<ClaimViewModel> ClaimsAwaitingMyApproval { get; set; } = new List<ClaimViewModel>();
    }
}



