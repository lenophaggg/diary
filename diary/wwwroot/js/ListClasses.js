document.addEventListener('DOMContentLoaded', function () {
    const lessonTypes = [
        { value: 'laboratoryworks', label: 'Лабораторные работы' },
        { value: 'practicalclasses', label: 'Практические занятия' },
        { value: 'seminars', label: 'Семинары' },
        { value: 'colloquiums', label: 'Коллоквиумы' },
        { value: 'lectures', label: 'Лекции'},
        { value: 'consultations', label: 'Консультации' }
    ];

    // Заполнение типов занятий для создания/редактирования занятия
    const lessonTypesContainer = document.getElementById('lessonTypes');
    lessonTypes.forEach(type => {
        const div = document.createElement('div');
        div.className = 'form-check';

        const radio = document.createElement('input');
        radio.type = 'radio';
        radio.id = type.value;
        radio.value = type.value;
        radio.name = 'lessonType';
        radio.className = 'form-check-input';

        const label = document.createElement('label');
        label.htmlFor = type.value;
        label.textContent = type.label;
        label.className = 'form-check-label';

        div.appendChild(radio);
        div.appendChild(label);
        lessonTypesContainer.appendChild(div);
    });

    // Обработчик формы создания/редактирования занятия
    const createForm = document.getElementById("createClassForm");
    createForm.addEventListener("submit", function (event) {
        event.preventDefault();
        event.stopPropagation();

        if (createForm.checkValidity()) {
            const classId = document.getElementById('classId').value;
            if (classId) {
                updateClass(classId); 
            } else {
                saveClass(); 
            }
        }
        createForm.classList.add("was-validated");
    }, false);
});

// Открыть модальное окно для создания занятия
function openCreateClassModal() {
    resetForm(); 
    const modal = new bootstrap.Modal(document.getElementById("createClassModal"));
    modal.show();
}

// Открыть модальное окно для редактирования занятия
function openEditClassModal(classId) {
    loadClassData(classId); 
    const modal = new bootstrap.Modal(document.getElementById("createClassModal"));
    modal.show();
}

// Функция для создания нового занятия
function saveClass() {
    const subjectName = document.getElementById('subjectName').value;
    const studyDuration = document.getElementById('studyDuration').value;
    const semester = document.getElementById('semester').value;
    const academicYear = document.getElementById('academicYear').value;
    const lessonType = document.querySelector('#lessonTypes input[type="radio"]:checked').value;

    $.ajax({
        url: '/Teacher/CreateClassWithoutGroup',
        type: 'POST',
        data: {
            subjectName: subjectName,
            studyDuration: studyDuration,
            semester: semester,
            academicYear: academicYear,
            lessonType: lessonType
        },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('Ошибка при создании занятия');
        }
    });
}

// Функция для редактирования существующего занятия
function updateClass(classId) {
    const subjectName = document.getElementById('subjectName').value;
    const studyDuration = document.getElementById('studyDuration').value;
    const semester = document.getElementById('semester').value;
    const academicYear = document.getElementById('academicYear').value;
    const lessonType = document.querySelector('#lessonTypes input[type="radio"]:checked').value;

    $.ajax({
        url: '/Teacher/UpdateClass',
        type: 'POST',
        data: {
            classId: classId,
            subjectName: subjectName,
            studyDuration: studyDuration,
            semester: semester,
            academicYear: academicYear,
            lessonType: lessonType
        },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('Ошибка при обновлении занятия');
        }
    });
}


function loadClassData(classId) {
    $.ajax({
        url: '/Teacher/GetClass',
        type: 'GET',
        data: {
            classId: classId
        },
        success: function (classData) {
            document.getElementById('classId').value = classData.classId;
            document.getElementById('subjectName').value = classData.subject;
            document.getElementById('studyDuration').value = classData.studyDuration;
            document.getElementById('semester').value = classData.semester;
            document.getElementById('academicYear').value = classData.academicYear;
            
            const selectedRadio = document.querySelector(`#lessonTypes input[type="radio"][value="${classData.lessonType}"]`);
            if (selectedRadio) {
                selectedRadio.checked = true;
            }
            document.getElementById('createClassModalLabel').textContent = 'Редактировать Занятие';
        },
        error: function () {
            alert('Ошибка при загрузке данных занятия');
        }
    });
}

// Сброс формы и заголовка перед открытием модального окна
function resetForm() {
    document.getElementById('classId').value = '';
    document.getElementById('subjectName').value = '';
    document.getElementById('studyDuration').value = '';
    document.getElementById('semester').value = '';
    document.getElementById('academicYear').value = '';
    document.querySelectorAll('#lessonTypes input[type="radio"]').forEach(radio => radio.checked = false);
    document.getElementById('createClassModalLabel').textContent = 'Создать Занятие';
}

// Удаление занятия
function deleteClass(classId) {
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