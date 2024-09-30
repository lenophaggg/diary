using diary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace diary.ViewModels
{
    public class StudentAbsenceViewModel
    {
        public StudentAbsenceRequestViewModel AbsenceRequest { get; set; }
        public List<StudentData> Students { get; set; } = new List<StudentData>();
        public List<string> GroupsNumbers { get; set; } = new List<string>();
        public bool CanEdit { get; set; }
    }

    public class StudentAbsenceRequestViewModel
    {
        public int? RequestId { get; set; }

        [Required(ErrorMessage = "Причина отсутствия обязательна")]
        public string Reason { get; set; }

        [Required(ErrorMessage = "Дата начала обязательна")]
        [DataType(DataType.Date, ErrorMessage = "Неверный формат даты")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Дата окончания обязательна")]
        [DataType(DataType.Date, ErrorMessage = "Неверный формат даты")]
        public DateTime EndDate { get; set; }

        public int StudentId { get; set; } // Идентификатор студента

        [Required(ErrorMessage = "ФИО студента обязательно")]
        public string StudentName { get; set; }

        [Required(ErrorMessage = "Выбор группы обязателен")]
        public string GroupNumber { get; set; }

        public AbsencesStatus? Status { get; set; }
    }
}
