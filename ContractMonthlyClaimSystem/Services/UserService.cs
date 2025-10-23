
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
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.OrderBy(u => u.Name).ToListAsync();
        }

        public async Task<List<User>> GetLecturersAsync()
        {
            return await _context.Users
                .Where(u => u.Role == UserRole.Lecturer)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<UserProfileViewModel> GetUserProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var profile = new UserProfileViewModel
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString(),
                Department = user.Department,
                CreatedDate = user.CreatedDate
            };

            if (user.Role == UserRole.Lecturer)
            {
                var claims = await _context.Claims
                    .Where(c => c.LecturerId == userId)
                    .ToListAsync();

                profile.TotalClaims = claims.Count;
                profile.TotalEarnings = claims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.TotalAmount);
            }
            else
            {
                var pendingApprovals = await _context.Claims
                    .Include(c => c.Approvals)
                    .Where(c => (c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview)
                               && !c.Approvals.Any(a => a.ApproverId == userId))
                    .CountAsync();

                profile.PendingApprovals = pendingApprovals;
            }

            return profile;
        }

        public async Task<bool> ValidatePasswordAsync(User user, string password)
        {
            // Simple password validation - in production, use proper hashing
            return user.PasswordHash == HashPassword(password);
        }

        private string HashPassword(string password)
        {
            // Simple hashing - use BCrypt or similar in production
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SALT"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
