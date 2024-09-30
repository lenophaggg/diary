using diary.Data;
using diary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using diary.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace diary
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHostedService<TypeWeekDownloadService>();

            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(@"/app/keys"));  // Папка для ключей


            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("applicationDbContextConnection")));

            builder.Services.AddDbContext<DiaryDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("diaryDbContextConnection")));

            builder.Services.AddDbContext<DiaryIdentityDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DiaryIdentityDbContextConnection")));

            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false; // Отключение требования подтверждения аккаунта
                options.User.RequireUniqueEmail = false; // Отключение требования уникальности email

                // Настройки пароля
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                // Настройки блокировки
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<DiaryIdentityDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.LoginPath = "/Home/Login";
                options.AccessDeniedPath = "/Home/AccessDenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(1); // Установите кратковременный срок действия куки по умолчанию

                options.Events.OnValidatePrincipal = async context =>
                {
                    var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                    var user = await userManager.GetUserAsync(context.Principal);
                    if (user == null || await userManager.IsLockedOutAsync(user))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync();
                    }
                    else
                    {
                        // Проверяем срок действия куки и продлеваем его
                        if (context.Properties.IsPersistent)
                        {
                            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
                        }
                        else
                        {
                            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1);
                        }
                        context.ShouldRenew = true;
                    }
                };
            });

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCookiePolicy();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "admin",
                pattern: "{controller=Admin}/{action=Index}/{id?}");

            app.MapControllerRoute(
               name: "teacher",
               pattern: "{controller=Teacher}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "grouphead",
                pattern: "{controller=GroupHead}/{action=Index}/{id?}");




            await app.RunAsync();
        }
    }
}
