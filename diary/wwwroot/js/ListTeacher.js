// ListTeacher.js

function fetchTeachers(searchTerm) {
    // Показать индикатор загрузки и скрыть основной контейнер
    document.getElementById("loader").style.display = "block";
    document.querySelector(".container-fluid").style.display = "none";

    $.ajax({
        url: '/Admin/FilterTeachers',
        type: 'GET',
        data: { searchTerm: searchTerm },
        success: function (data) {
            // Обновить содержимое таблицы преподавателей
            $('#teachersTableContainer').html(data);

            // Проверить, все ли изображения загружены
            checkImagesLoaded();
        },
        error: function (xhr, status, error) {
            alert('Ошибка при поиске преподавателей.');
            console.error('AJAX Error:', status, error);
            // Скрыть индикатор загрузки и показать основной контейнер
            document.getElementById("loader").style.display = "none";
            document.querySelector(".container-fluid").style.display = "block";
        }
    });
}

/**
 * Функция для проверки, все ли изображения загружены.
 */
function checkImagesLoaded() {
    var container = document.querySelector(".container-fluid");
    var images = container.querySelectorAll("img");
    var totalImages = images.length;
    var loadedImages = 0;

    function checkAllImagesLoaded() {
        loadedImages++;
        if (loadedImages === totalImages) {
            // Скрыть индикатор загрузки и показать основной контейнер
            document.getElementById("loader").style.display = "none";
            container.style.display = "block";
        }
    }

    if (totalImages > 0) {
        // Добавить обработчики событий для каждого изображения
        images.forEach(function (img) {
            if (img.complete) {
                checkAllImagesLoaded();
            } else {
                img.addEventListener("load", checkAllImagesLoaded);
                img.addEventListener("error", checkAllImagesLoaded);
            }
        });
    } else {
        // Если изображений нет, сразу показать основной контейнер
        document.getElementById("loader").style.display = "none";
        container.style.display = "block";
    }
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
        if (e.which == 13) { // 13 - код клавиши Enter
            $('#searchTeacherBtn').click();
            return false; // Предотвращает отправку формы
        }
    });

    // Обработчик закрытия модального окна для сброса формы
    $('#addTeacherModal').on('hidden.bs.modal', function () {
        $('#addTeacherForm')[0].reset();
    });

    // Логика для отображения страницы после загрузки всех изображений при начальной загрузке
    checkImagesLoaded();
});