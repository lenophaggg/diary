document.addEventListener('DOMContentLoaded', function () {
    const lessonTypes = [
        { value: 'laboratoryworks', label: 'Лабораторные работы' },
        { value: 'practicalclasses', label: 'Практические занятия' },
        { value: 'seminars', label: 'Семинары' },
        { value: 'colloquiums', label: 'Коллоквиумы' },
        { value: 'lectures', label: 'Лекции' },
        { value: 'consultations', label: 'Консультации' }
    ];

    // Populate lesson type radio buttons
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

    // Обновление текста типов занятий на русском языке в таблице
    const lessonTypeMapping = {
        'laboratoryworks': 'Лабораторные работы',
        'practicalclasses': 'Практические занятия',
        'seminars': 'Семинары',
        'colloquiums': 'Коллоквиумы',
        'lectures': 'Лекции',
        'consultations': 'Консультации'
    };

    document.querySelectorAll('.lesson-type').forEach(function (element) {
        const type = element.dataset.lessonType;
        if (lessonTypeMapping[type]) {
            element.textContent = lessonTypeMapping[type];
        } else {
            element.textContent = 'Неизвестный тип'; // В случае если тип не найден
        }
    });

    // Populate the instructor datalist with options
    populateInstructorDatalist();

    // Form validation and submission handler
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

// Function to populate the datalist with instructors
function populateInstructorDatalist() {
    $.ajax({
        url: '/GroupHead/GetInstructors', // Endpoint to fetch instructors
        type: 'GET',
        success: function (instructors) {
            const instructorList = document.getElementById('instructorList');
            instructors.forEach(instructor => {
                const option = document.createElement('option');
                option.value = instructor.nameContact; // Populate with instructor's name
                option.setAttribute('data-id', instructor.idContact); // Store instructor's ID as a data attribute
                instructorList.appendChild(option);
            });
        },
        error: function () {
            alert('Ошибка при загрузке списка преподавателей');
        }
    });
}

// Open modal for creating a new class
function openCreateClassModal() {
    resetForm();
    document.getElementById('createClassModalLabel').textContent = 'Создать Занятие';
    const modal = new bootstrap.Modal(document.getElementById("createClassModal"));
    modal.show();
}

// Open modal for editing an existing class
function openEditClassModal(classId) {
    resetForm();
    loadClassData(classId);
    const modal = new bootstrap.Modal(document.getElementById("createClassModal"));
    modal.show();
}

// Function to create a new class
function saveClass() {
    const subjectName = document.getElementById('subjectName').value;
    const studyDuration = document.getElementById('studyDuration').value;
    const semester = document.getElementById('semester').value;
    const academicYear = document.getElementById('academicYear').value;
    const lessonType = document.querySelector('#lessonTypes input[type="radio"]:checked').value;
    const instructorName = document.getElementById('instructorName').value;

    $.ajax({
        url: '/GroupHead/CreateClass',
        type: 'POST',
        data: {
            subjectName: subjectName,
            studyDuration: studyDuration,
            semester: semester,
            academicYear: academicYear,
            lessonType: lessonType,
            instructorName: instructorName
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

// Function to update an existing class
function updateClass(classId) {
    const subjectName = document.getElementById('subjectName').value;
    const studyDuration = document.getElementById('studyDuration').value;
    const semester = document.getElementById('semester').value;
    const academicYear = document.getElementById('academicYear').value;
    const lessonType = document.querySelector('#lessonTypes input[type="radio"]:checked').value;
    const instructorName = document.getElementById('instructorName').value;

    $.ajax({
        url: '/GroupHead/UpdateClass',
        type: 'POST',
        data: {
            classId: classId,
            subjectName: subjectName,
            studyDuration: studyDuration,
            semester: semester,
            academicYear: academicYear,
            lessonType: lessonType,
            instructorName: instructorName
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

// Load class data into the form for editing
function loadClassData(classId) {
    $.ajax({
        url: '/GroupHead/GetClass',
        type: 'GET',
        data: {
            classId: classId
        },
        success: function (classData) {
            if (classData) {
                document.getElementById('classId').value = classData.classId;
                document.getElementById('subjectName').value = classData.subject || '';
                document.getElementById('studyDuration').value = classData.studyDuration || '';
                document.getElementById('semester').value = classData.semester || '';
                document.getElementById('academicYear').value = classData.academicYear || '';

                const selectedRadio = document.querySelector(`#lessonTypes input[type="radio"][value="${classData.lessonType}"]`);
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

// Reset form and modal title before opening the modal
function resetForm() {
    document.getElementById('classId').value = '';
    document.getElementById('subjectName').value = '';
    document.getElementById('instructorName').value = '';
    document.getElementById('studyDuration').value = '';
    document.getElementById('semester').value = '';
    document.getElementById('academicYear').value = '';
    document.querySelectorAll('#lessonTypes input[type="radio"]').forEach(radio => radio.checked = false);
    document.getElementById('createClassModalLabel').textContent = 'Создать Занятие';
}
