// ListTeacher.js

function fetchTeachers(searchTerm, page = 1, pageSize = 10) {
    $.ajax({
        url: '/Admin/FilterTeachers',
        type: 'GET',
        data: { searchTerm: searchTerm, page: page, pageSize: pageSize },
        success: function (data) {
            $('#teachersTableContainer').html(data);
            // Больше не нужно скрывать/показывать контейнер, так как loader удален
        },
        error: function (xhr, status, error) {
            alert('Ошибка при поиске преподавателей.');
            console.error('AJAX Error:', status, error);
        }
    });
}

/**
 * Функция для добавления нового преподавателя.
 */
function addTeacher() {
    var teacherName = $('#teacherName').val().trim();
    var teacherUniversityId = $('#teacherUniversityId').val().trim();

    // Валидация на клиентской стороне
    if (!teacherName || !teacherUniversityId) {
        alert('Заполните все поля.');
        return;
    }

    $.ajax({
        url: '/Admin/AddTeacher',
        type: 'POST',
        data: {
            teacherName: teacherName,
            teacherUniversityId: teacherUniversityId
        },
        success: function (response) {
            if (response.success) {
                // Закрыть модальное окно и обновить список преподавателей
                $('#addTeacherModal').modal('hide');
                fetchTeachers($('#searchTeacherInput').val());
                $('#addTeacherForm')[0].reset();
            } else {
                alert(response.message);
            }
        },
        error: function (xhr, status, error) {
            alert('Не удалось добавить преподавателя.');
            console.error('AJAX Error:', status, error);
        }
    });
}

/**
 * Функция для открытия модального окна добавления преподавателя.
 */
function openAddTeacherModal() {
    $('#addTeacherModal').modal('show');
}

$(document).ready(function () {
    // Обработчик клика на кнопку поиска
    $('#searchTeacherBtn').on('click', function () {
        var searchTerm = $('#searchTeacherInput').val().trim();
        fetchTeachers(searchTerm);
    });

    // Обработчик нажатия клавиши Enter в поле поиска
    $('#searchTeacherInput').on('keypress', function (e) {
        if (e.which === 13) { // 13 - код клавиши Enter
            $('#searchTeacherBtn').click();
            return false; // Предотвращает отправку формы
        }
    });

    // Обработчик закрытия модального окна для сброса формы
    $('#addTeacherModal').on('hidden.bs.modal', function () {
        $('#addTeacherForm')[0].reset();
    });
});