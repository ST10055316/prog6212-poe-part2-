
using ContractMonthlyClaimSystem.Models.ViewModels;
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
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetLecturersAsync();
        Task<UserProfileViewModel> GetUserProfileAsync(int userId);
        Task<bool> ValidatePasswordAsync(User user, string password);
    }

}
