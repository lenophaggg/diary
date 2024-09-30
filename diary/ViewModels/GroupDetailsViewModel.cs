using diary.Models;
using System;
using System.Collections.Generic;

namespace diary.ViewModels
{
    public class GroupDetailsViewModel
    {
        public string GroupNumber { get; set; }
        public List<StudentData> Students { get; set; }
        public StudentData GroupHead { get; set; }
    }
}
