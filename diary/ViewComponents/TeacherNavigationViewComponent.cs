using Microsoft.AspNetCore.Mvc;

namespace diary.ViewComponents
{
    [ViewComponent(Name = "TeacherNavigation")]
    public class TeacherNavigationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("Default");
        }
    }
}
