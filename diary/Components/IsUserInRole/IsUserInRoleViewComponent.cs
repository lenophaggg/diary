using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace diary.Components
{
    public class IsUserInRoleViewComponent : ViewComponent
    {
        private readonly UserManager<IdentityUser> _userManager;

        public IsUserInRoleViewComponent(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var isInRole = await _userManager.IsInRoleAsync(user, roleName);
                ViewData["roleName"] = roleName;
                return View(isInRole);
            }
            return View(false);
        }
    }
}