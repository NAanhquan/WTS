using AttendanceTrackingSystem.Services.Implementations;
using AttendanceTrackingSystem.Services.Interfaces;
using AttendenceTrackingSystem.Models;
using AttendenceTrackingSystem.Services;
using AttendenceTrackingSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AttendenceTrackingSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddDbContext<AttendanceTrackingSystemContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IUserManagementService, UserManagementService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IAttendanceService, AttendanceService>();
            builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            builder.Services.AddRazorPages();
            builder.Services.AddLogging();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.MapGet("/", context =>
            {
                context.Response.Redirect("/Login");
                return Task.CompletedTask;
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
