using ContractMonthlyClaimSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies; // Make sure this is added

namespace ContractMonthlyClaimSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register the IClaimService and its implementation
            builder.Services.AddScoped<IClaimService, ClaimService>();

            builder.Services.AddScoped<IUserService, UserService>();

            // ... other services
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // FIX: Use the constant CookieAuthenticationDefaults.AuthenticationScheme
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, config =>
                {
                    config.Cookie.Name = "UserLoginCookie";
                    config.LoginPath = "/Account/Login";
                    config.AccessDeniedPath = "/Account/AccessDenied";
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // The order is important: UseAuthentication must be before UseAuthorization
            app.UseAuthentication();
            app.UseAuthorization();

            // This is the key part for routing to controllers
            app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}