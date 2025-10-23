
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
    public interface IAuthenticationService
    {
        Task<AuthResult> SignInAsync(string username, string password, bool rememberMe);
        Task SignOutAsync();
    }
}
