using diary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace diary.ViewComponents
{
    [ViewComponent(Name="Header")]
    public class HeaderViewComponent : ViewComponent
    {
        private readonly IConfiguration _configuration;

        public HeaderViewComponent(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var typeWeek = _configuration["ScheduleOptions:TypeWeek"];
            string shortWeekType = typeWeek.ToLower() switch
            {
                "верхняя неделя" => "верхняя",
                "нижняя неделя" => "нижняя",
                _ => typeWeek
            };

            var currentDate = DateTime.Now.ToString("dd.MM");
            var headerContent = $"{shortWeekType} | {currentDate}";

            return View("Default", headerContent);
        }
    }
}
