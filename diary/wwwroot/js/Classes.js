document.addEventListener('DOMContentLoaded', function () {

    // Отображаем индикатор загрузки
    document.getElementById('loader').style.display = 'block';

    // Инициализируем страницу
    initializePage();

    function initializePage() {

        // Сопоставление типов занятий с отображаемыми названиями
        const lessonTypeMapping = {
            'laboratoryworks': 'Лабораторные работы',
            'practicalclasses': 'Практические занятия',
            'seminars': 'Семинары',
            'colloquiums': 'Коллоквиумы',
            'lectures': 'Лекции',
            'consultations': 'Консультации'
        };

        // Заполняем радио-кнопки типов занятий
        populateLessonTypes();

        function populateLessonTypes() {
            const lessonTypesContainer = document.getElementById('lessonTypes');
            if (lessonTypesContainer) {
                Object.keys(lessonTypeMapping).forEach(type => {
                    const div = document.createElement('div');
                    div.className = 'form-check';

                    const radio = document.createElement('input');
                    radio.type = 'radio';
                    radio.id = type;
                    radio.value = type;
                    radio.name = 'lessonType';
                    radio.className = 'form-check-input';

                    const label = document.createElement('label');
                    label.htmlFor = type;
                    label.textContent = lessonTypeMapping[type];
                    label.className = 'form-check-label';

                    div.appendChild(radio);
                    div.appendChild(label);
                    lessonTypesContainer.appendChild(div);
                });
            }
        }

        // Обновляем отображение типов занятий в таблице
        updateLessonTypes();

        function updateLessonTypes() {
            const lessonTypeElements = document.querySelectorAll('.lesson-type');
            if (lessonTypeElements.length > 0) {
                lessonTypeElements.forEach(function (element) {
                    const type = element.dataset.lessonType;
                    if (type && lessonTypeMapping[type.toLowerCase()]) {
                        element.textContent = lessonTypeMapping[type.toLowerCase()];
                    } else {
                        element.textContent = 'Неизвестный тип';
                    }
                });
            }
        }

        // Заполняем список преподавателей
        populateInstructorDatalist();

        function populateInstructorDatalist() {
            $.ajax({
                url: '/Shared/GetInstructors',
                type: 'GET',
                success: function (instructors) {
                    const instructorList = document.getElementById('instructorList');
                    if (instructorList) {
                        instructorList.innerHTML = '';
                        instructors.forEach(instructor => {
                            const option = document.createElement('option');
                            option.value = instructor.nameContact;
                            option.setAttribute('data-id', instructor.idContact);
                            instructorList.appendChild(option);
                        });
                    }
                },
                error: function () {
                    alert('Ошибка при загрузке списка преподавателей');
                }
            });
        }

        // Обработка кнопки поиска группы (для Админа)
        const searchGroupBtn = document.getElementById("searchGroupBtn");
        if (searchGroupBtn) {
            searchGroupBtn.addEventListener("click", function () {
                const groupNumber = document.getElementById("searchGroupInput").value.trim();

                // Отображаем индикатор загрузки во время поиска
                document.getElementById("loader").style.display = "block";
                document.getElementById("contentContainer").style.display = "none";

                $.ajax({
                    url: '/Shared/FilterClasses',
                    type: 'GET',
                    data: { groupNumber: groupNumber },
                    success: function (data) {
                        $('#classesTableContainer').html(data);

                        // Обновляем отображение типов занятий после обновления таблицы
                        updateLessonTypes();

                        // Скрываем индикатор загрузки и отображаем контент
                        document.getElementById("loader").style.display = "none";
                        document.getElementById("contentContainer").style.display = "block";
                    },
                    error: function () {
                        alert('Ошибка при поиске занятий.');
                        document.getElementById("loader").style.display = "none";
                        document.getElementById("contentContainer").style.display = "block";
                    }
                });
            });
        }

        // Валидация формы и отправка данных для создания/обновления занятия
        const createForm = document.getElementById("createClassForm");
        if (createForm) {
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
        }

        // Скрываем индикатор загрузки и отображаем контент после инициализации
        document.getElementById('loader').style.display = 'none';
        document.getElementById('contentContainer').style.display = 'block';
    }

    // Функция для открытия модального окна создания занятия
    window.openCreateClassModal = function () {
        resetForm();
        document.getElementById('createClassModalLabel').textContent = 'Создать Занятие';
        const modal = new bootstrap.Modal(document.getElementById("createClassModal"));
        modal.show();
    }

    // Функция для открытия модального окна редактирования занятия
    window.openEditClassModal = function (classId) {
        resetForm();
        const modal = new bootstrap.Modal(document.getElementById("createClassModal"));
        modal.show();
        loadClassData(classId);
    }

    // Функция для сохранения нового занятия
    function saveClass() {
        const subjectName = document.getElementById('subjectName').value.trim();
        const semester = document.getElementById('semester').value;
        const academicYear = document.getElementById('academicYear').value.trim();
        const lessonTypeElement = document.querySelector('#lessonTypes input[type="radio"]:checked');
        const instructorName = document.getElementById('instructorName').value.trim();
        const groupNumber = document.getElementById('groupNumber').value.trim();

        if (!lessonTypeElement) {
            alert('Пожалуйста, выберите тип занятия.');
            return;
        }

        const lessonType = lessonTypeElement.value;

        $.ajax({
            url: '/Shared/CreateClass',
            type: 'POST',
            data: {
                subjectName: subjectName,
                semester: semester,
                academicYear: academicYear,
                lessonType: lessonType,
                instructorName: instructorName,
                groupNumber: groupNumber
            },
            success: function (response) {
                if (response.success) {
                    location.reload();
                } else {
                    alert(response.message || 'Ошибка при создании занятия.');
                }
            },
            error: function () {
                alert('Ошибка при создании занятия');
            }
        });
    }

    // Функция для обновления существующего занятия
    function updateClass(classId) {
        const subjectName = document.getElementById('subjectName').value.trim();
        const semester = document.getElementById('semester').value;
        const academicYear = document.getElementById('academicYear').value.trim();
        const lessonTypeElement = document.querySelector('#lessonTypes input[type="radio"]:checked');
        const instructorName = document.getElementById('instructorName').value.trim();
        const groupNumber = document.getElementById('groupNumber').value.trim();

        if (!lessonTypeElement) {
            alert('Пожалуйста, выберите тип занятия.');
            return;
        }

        const lessonType = lessonTypeElement.value;

        $.ajax({
            url: '/Shared/UpdateClass',
            type: 'POST',
            data: {
                classId: classId,
                subjectName: subjectName,
                semester: semester,
                academicYear: academicYear,
                lessonType: lessonType,
                instructorName: instructorName,
                groupNumber: groupNumber
            },
            success: function (response) {
                if (response.success) {
                    location.reload();
                } else {
                    alert(response.message || 'Ошибка при обновлении занятия.');
                }
            },
            error: function () {
                alert('Ошибка при обновлении занятия');
            }
        });
    }

    // Функция для загрузки данных занятия в форму редактирования
    function loadClassData(classId) {
        $.ajax({
            url: '/Shared/GetClass',
            type: 'GET',
            data: {
                classId: classId
            },
            success: function (classData) {
                if (classData) {
                    document.getElementById('classId').value = classData.classId;
                    document.getElementById('subjectName').value = classData.subject || '';
                    document.getElementById('semester').value = classData.semester || '';
                    document.getElementById('academicYear').value = classData.academicYear || '';
                    document.getElementById('groupNumber').value = classData.groupNumber || '';

                    const selectedRadio = document.querySelector(`#lessonTypes input[type="radio"][value="${classData.lessonType.toLowerCase()}"]`);
                    if (selectedRadio) {
                        selectedRadio.checked = true;
                    }

                    document.getElementById('instructorName').value = classData.instructorName || '';

                    document.getElementById('createClassModalLabel').textContent = 'Редактировать Занятие';

                } else {
                    alert('Ошибка: данные занятия не найдены.');
                }
            },
            error: function () {
                alert('Ошибка при загрузке данных занятия');
            }
        });
    }

    // Сброс формы перед открытием модального окна
    function resetForm() {
        const createForm = document.getElementById("createClassForm");
        if (createForm) {
            createForm.classList.remove("was-validated");
            createForm.reset();
        }
        document.getElementById('classId').value = '';
        document.getElementById('createClassModalLabel').textContent = 'Создать Занятие';
    }

    // Функция для удаления занятия
    window.deleteClass = function (classId) {
        if (confirm('Вы уверены, что хотите удалить занятие?')) {
            $.ajax({
                url: '/Shared/DeleteClass',
                type: 'POST',
                data: { classId: classId },
                success: function (result) {
                    if (result.success) {
                        location.reload();
                    } else {
                        alert(result.message || 'Ошибка при удалении занятия.');
                    }
                },
                error: function () {
                    alert('Ошибка при удалении занятия.');
                }
            });
        }
    }

});
