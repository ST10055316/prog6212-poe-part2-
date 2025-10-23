using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ClaimSearchViewModel
    {
        public string? SearchText { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; }

        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Lecturer")]
        public int? LecturerId { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalResults { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);

        public List<ClaimViewModel> Results { get; set; } = new List<ClaimViewModel>();
        public List<User> AvailableLecturers { get; set; } = new List<User>();

        public bool HasActiveFilters =>
            !string.IsNullOrEmpty(SearchText) ||
            !string.IsNullOrEmpty(Status) || // Fixed: string doesn't have HasValue
            FromDate.HasValue ||
            ToDate.HasValue ||
            LecturerId.HasValue;
    }
}