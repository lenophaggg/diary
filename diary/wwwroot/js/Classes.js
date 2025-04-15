document.addEventListener('DOMContentLoaded', () => {
    const loader = document.getElementById('loader');
    const contentContainer = document.getElementById('contentContainer');

    initializePage();

    function initializePage() {
        showLoader(true);
        populateLessonTypes();
        populateInstructorDatalist();
        attachEventListeners();
        updateLessonTypes();
        showLoader(false);
    }

    function showLoader(show) {
        loader.style.display = show ? 'block' : 'none';
        contentContainer.style.display = show ? 'none' : 'block';
    }

    function populateLessonTypes() {
        const lessonTypes = {
            'laboratoryworks': 'Лабораторные работы',
            'practicalclasses': 'Практические занятия',
            'seminars': 'Семинары',
            'colloquiums': 'Коллоквиумы',
            'lectures': 'Лекции',
            'consultations': 'Консультации'
        };

        const lessonTypesContainer = document.getElementById('lessonTypes');
        if (!lessonTypesContainer) return;

        lessonTypesContainer.innerHTML = Object.entries(lessonTypes)
            .map(([key, value]) => `
                <div class="form-check">
                    <input type="radio" class="form-check-input" id="${key}" name="lessonType" value="${key}" required>
                    <label class="form-check-label" for="${key}">${value}</label>
                </div>
            `)
            .join('');
    }

    function populateInstructorDatalist() {
        fetch('/Shared/GetInstructors')
            .then(response => {
                if (!response.ok) throw new Error('Ошибка при загрузке преподавателей');
                return response.json();
            })
            .then(data => {
                const instructorList = document.getElementById('instructorList');
                if (instructorList) {
                    instructorList.innerHTML = data.map(i => `<option value="${i.nameContact}" data-id="${i.idContact}"></option>`).join('');
                }
            })
            .catch(error => alert(error.message));
    }

    function attachEventListeners() {
        const createForm = document.getElementById('createClassForm');
        if (createForm) createForm.addEventListener('submit', handleFormSubmit);

        const filterForm = document.getElementById('filterForm');
        if (filterForm) filterForm.addEventListener('submit', handleFilterSubmit);
    }

    function handleFormSubmit(e) {
        e.preventDefault();
        const form = e.target;

        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        const classId = document.getElementById('classId').value;
        const action = classId ? 'UpdateClass' : 'CreateClass';
        const data = new FormData();

        // Common fields for both roles
        data.append('subjectName', document.getElementById('subjectName').value.trim());
        data.append('instructorName', document.getElementById('instructorName').value.trim());
        const lessonType = form.querySelector('input[name="lessonType"]:checked')?.value;
        if (!lessonType) {
            alert('Пожалуйста, выберите тип занятия.');
            return;
        }
        data.append('lessonType', lessonType);

        // Admin-only fields
        const groupNumberEl = document.getElementById('groupNumber');
        const semesterEl = document.getElementById('semester');
        const academicYearEl = document.getElementById('academicYear');
        if (groupNumberEl) data.append('groupNumber', groupNumberEl.value.trim());
        if (semesterEl) data.append('semester', semesterEl.value);
        if (academicYearEl) data.append('academicYear', academicYearEl.value.trim());

        // Add classId for updates
        if (classId) data.append('classId', classId);

        fetch(`/Shared/${action}`, {
            method: 'POST',
            body: new URLSearchParams(data)
        })
            .then(response => {
                if (!response.ok) throw new Error(`Ошибка при ${classId ? 'обновлении' : 'создании'} занятия`);
                return response.json();
            })
            .then(res => {
                if (res.success) {
                    location.reload();
                } else {
                    alert(res.message || `Ошибка при ${classId ? 'обновлении' : 'создании'} занятия`);
                }
            })
            .catch(error => alert(error.message));
    }

    function handleFilterSubmit(e) {
        e.preventDefault();
        const form = e.target;

        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }

        const groupNumber = document.getElementById('searchGroupInput')?.value.trim() || '';
        const semester = document.getElementById('searchSemesterInput')?.value.trim() || '';
        const academicYear = document.getElementById('searchAcademicYearInput')?.value.trim() || '';

        const params = new URLSearchParams();
        if (groupNumber) params.append('groupNumber', groupNumber);
        if (semester) params.append('semester', semester);
        if (academicYear) params.append('academicYear', academicYear);

        showLoader(true);

        fetch(`/Shared/FilterClasses?${params.toString()}`)
            .then(res => {
                if (!res.ok) throw new Error('Ошибка при фильтрации занятий');
                return res.text();
            })
            .then(html => {
                document.getElementById('classesTableContainer').innerHTML = html;
                updateLessonTypes();
                showLoader(false);
            })
            .catch(error => {
                alert(error.message);
                showLoader(false);
            });
    }

    window.openCreateClassModal = function () {
        resetForm();
        new bootstrap.Modal(document.getElementById('createClassModal')).show();
    };

    window.openEditClassModal = function (classId) {
        resetForm();
        fetch(`/Shared/GetClass?classId=${classId}`)
            .then(res => {
                if (!res.ok) throw new Error('Ошибка загрузки данных занятия');
                return res.json();
            })
            .then(data => {
                document.getElementById('classId').value = data.classId;
                document.getElementById('subjectName').value = data.subject || '';
                document.getElementById('instructorName').value = data.instructorName || '';

                const groupNumberEl = document.getElementById('groupNumber');
                const semesterEl = document.getElementById('semester');
                const academicYearEl = document.getElementById('academicYear');
                if (groupNumberEl) groupNumberEl.value = data.groupNumber || '';
                if (semesterEl) semesterEl.value = data.semester || '';
                if (academicYearEl) academicYearEl.value = data.academicYear || '';

                const lessonTypeRadio = document.querySelector(`#lessonTypes input[value="${data.lessonType?.toLowerCase()}"]`);
                if (lessonTypeRadio) lessonTypeRadio.checked = true;

                document.getElementById('createClassModalLabel').textContent = 'Редактировать занятие';
                new bootstrap.Modal(document.getElementById('createClassModal')).show();
            })
            .catch(error => alert(error.message));
    };

    window.deleteClass = function (classId) {
        if (!confirm('Удалить занятие?')) return;

        fetch('/Shared/DeleteClass', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `classId=${classId}`
        })
            .then(res => {
                if (!res.ok) throw new Error('Ошибка при удалении занятия');
                return res.json();
            })
            .then(res => res.success ? location.reload() : alert(res.message || 'Ошибка при удалении'))
            .catch(error => alert(error.message));
    };

    function resetForm() {
        const form = document.getElementById('createClassForm');
        if (form) {
            form.reset();
            form.classList.remove('was-validated');
            document.getElementById('classId').value = '';
            document.getElementById('createClassModalLabel').textContent = 'Создать занятие';
        }
    }

    function updateLessonTypes() {
        const lessonTypeMapping = {
            'laboratoryworks': 'Лабораторные работы',
            'practicalclasses': 'Практические занятия',
            'seminars': 'Семинары',
            'colloquiums': 'Коллоквиумы',
            'lectures': 'Лекции',
            'consultations': 'Консультации'
        };

        document.querySelectorAll('.lesson-type').forEach(el => {
            const type = el.getAttribute('data-lesson-type')?.toLowerCase();
            el.textContent = lessonTypeMapping[type] || 'Неизвестный тип';
        });
    }
});