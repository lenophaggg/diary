// Path: Data/DbInitializer.cs
using diary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace diary.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<DiaryIdentityDbContext>();
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                context.Database.EnsureCreated();

                // Роли для создания
                string[] roleNames = { "GroupHead", "Admin", "Teacher" };
                IdentityResult roleResult;

                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Создание пользователя администратора
                if (userManager.Users.All(u => u.UserName != "admin"))
                {
                    var adminUser = new IdentityUser
                    {
                        UserName = "admin",
                        // Дополнительные свойства можно задать здесь
                    };

                    var adminPassword = "@dminAdmin228"; // Рекомендуется безопасное хранение пароля

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
        }
    }
}
