using Microsoft.AspNetCore.Mvc;

namespace diary.ViewComponents
{
    [ViewComponent(Name = "AdminNavigation")]
    public class AdminNavigationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("Default");
        }
    }
}
