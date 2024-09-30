using Microsoft.AspNetCore.Mvc;

namespace diary.ViewComponents
{
    [ViewComponent(Name = "GroupHeadNavigation")]
    public class GroupHeadNavigationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("Default");
        }
    }
}
