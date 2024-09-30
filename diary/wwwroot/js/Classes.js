
document.addEventListener('DOMContentLoaded', function () {

    function loadClasses() {
        $.ajax({
            url: '/Teacher/GetClasses', // Метод, возвращающий список классов
            type: 'GET',
            success: function (response) {
                const classSelect = document.getElementById('classId');
                classSelect.innerHTML = '<option value="" selected disabled>Выберите занятие...</option>'; // Сбросить список
                response.forEach(function (classData) {
                    const option = document.createElement('option');
                    option.value = classData.classId;
                    option.textContent = `${classData.subject} (${classData.academicYear} - ${classData.semester} сем.)`;
                    classSelect.appendChild(option);
                });
            },
            error: function () {
                alert('Ошибка при загрузке списка занятий');
            }
        });
    }

    // Заполнение типа занятий в таблице
    const lessonTypeMapping = {
        'laboratoryworks': 'Лабораторные работы',
        'practicalclasses': 'Практические занятия',
        'seminars': 'Семинары',
        'colloquiums': 'Коллоквиумы',
        'consultations': 'Консультации',
        'lectures': 'Лекции'
    };

    document.querySelectorAll('.lesson-type').forEach(function (element) {
        const type = element.dataset.lessonType;
        if (lessonTypeMapping[type]) {
            element.textContent = lessonTypeMapping[type];
        } else {
            console.log(`Unknown type: ${type}`);
        }
    });  
        
    // Функция удаления занятия
    window.deleteClass = function (classId) {
        if (confirm('Вы уверены, что хотите удалить занятие?')) {
            $.ajax({
                url: '/Teacher/DeleteClass',
                type: 'POST',
                data: { classId: classId },
                success: function (result) {
                    if (result.success) {
                        location.reload();
                    } else {
                        alert('Ошибка при удалении занятия.');
                    }
                },
                error: function () {
                    alert('Ошибка при удалении занятия.');
                }
            });
        }
    }


    // Обработчик формы привязки занятия к группе
    const assignForm = document.getElementById("assignClassForm");
    if (assignForm) {
        assignForm.addEventListener("submit", function (event) {
            event.preventDefault();
            event.stopPropagation();
            if (assignForm.checkValidity()) {
                assignClassToGroup();
            }
            assignForm.classList.add("was-validated");
        }, false);
    }

    // Открытие модального окна привязки занятия к группе
    $('#assignClassModal').on('show.bs.modal', function () {
        loadClasses();
        
    });

    // Привязка занятия к группе
    function assignClassToGroup() {
        const classId = document.getElementById('classId').value;
        const groupNumber = document.getElementById('groupNumber').value;

        $.ajax({
            url: '/Teacher/AssignClassToGroup',
            type: 'POST',
            data: {
                classId: classId,
                groupNumber: groupNumber
            },
            success: function (response) {
                if (response.success) {
                    location.reload();
                } else {
                    alert(response.message);
                }
            },
            error: function () {
                alert('Ошибка при привязке занятия к группе');
            }
        });
    }    

    // Функция открытия модального окна привязки занятия к группе
    window.openAssignClassModal = function () {
        const modal = new bootstrap.Modal(document.getElementById("assignClassModal"));
        modal.show();
    };
});
