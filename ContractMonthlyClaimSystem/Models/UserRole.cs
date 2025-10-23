using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models
{
    public enum UserRole
    {
        Lecturer,
        ProgrammeCoordinator,
        AcademicManager
    }
}
