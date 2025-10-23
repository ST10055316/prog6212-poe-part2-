
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace ContractMonthlyClaimSystem.Services
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public User User { get; set; }
    }
}
