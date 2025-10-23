using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models
{
    public class ClaimSummaryDto
    {
        public int ClaimId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int Year { get; internal set; }
        public int Month { get; internal set; }
       public int ClaimCount { get; set; }
    }
}
