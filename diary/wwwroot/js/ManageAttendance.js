document.addEventListener('DOMContentLoaded', function () {
    const lessonTypes = [
        { value: 'laboratoryworks', label: 'Лабораторные работы' },
        { value: 'practicalclasses', label: 'Практические занятия' },
        { value: 'seminars', label: 'Семинары' },
        { value: 'colloquiums', label: 'Коллоквиумы' },
        { value: 'lectures', label: 'Лекции' },
        { value: 'consultations', label: 'Консультации' }
    ];

    const lessonTypeElement = document.querySelector('.lesson-type');
    if (lessonTypeElement) {
        const lessonTypeValue = lessonTypeElement.getAttribute('data-lesson-type');
        const matchedLessonType = lessonTypes.find(type => type.value === lessonTypeValue);
        if (matchedLessonType) {
            lessonTypeElement.textContent = matchedLessonType.label;
        }
    }

    const addColumnButton = document.getElementById("addColumnButton");
    if (addColumnButton) {
        addColumnButton.addEventListener("click", addSessionColumn);
    }

    const attendanceTable = document.getElementById("attendanceTable");
    if (attendanceTable) {
        attendanceTable.addEventListener("click", handleAttendanceActions);
    }

    // Обработка кнопки "Экспорт в Excel" для администраторов
    const exportButton = document.getElementById("exportToExcel");
    if (exportButton) {
        exportButton.addEventListener("click", function (event) {
            event.preventDefault();
            window.location.href = exportButton.getAttribute('data-export-url');
        });
    }
});

// Функция добавления новой колонки без сохранения в системе
function addSessionColumn() {
    const table = document.getElementById("attendanceTable");
    const newDateInput = document.getElementById("newDateInput");
    const newDate = newDateInput.value;

    if (!newDate) {
        alert("Пожалуйста, выберите дату.");
        return;
    }

    const existingSessionNumbers = Array.from(document.querySelectorAll(`th[data-date='${newDate}']`))
        .map(th => parseInt(th.getAttribute('data-session'), 10));
    const newSessionNumber = existingSessionNumbers.length > 0 ? Math.max(...existingSessionNumbers) + 1 : 1;

    const newHeader = document.createElement("th");
    newHeader.className = "align-middle text-center";
    newHeader.style.minWidth = "150px";
    newHeader.style.maxWidth = "250px";
    newHeader.setAttribute('data-date', newDate);
    newHeader.setAttribute('data-session', newSessionNumber);
    newHeader.innerHTML = `
        <div class="session-header">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                ${new Date(newDate).toLocaleDateString()} / ${newSessionNumber}
            </div>
            <div class="text-warning">не сохранено</div>
            <div class="d-flex justify-content-center mt-2">
                <button type="button" class="btn btn-sm btn-secondary save-attendance" data-date="${newDate}" data-session="${newSessionNumber}">Сохранить</button>
                
            </div>
        </div>
    `;

    // Вставка новой колонки перед последней фиксированной колонкой "Посещаемость"
    const beforeLastColumnIndex = table.rows[0].cells.length - 2;
    table.rows[0].insertBefore(newHeader, table.rows[0].cells[beforeLastColumnIndex]);

    for (let i = 1; i < table.rows.length; i++) {
        const cell = document.createElement("td");
        cell.className = "text-center";
        cell.style.maxWidth = "250px";
        const checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.disabled = false;

        checkbox.classList.add("attendance-checkbox", "form-check-input");

        checkbox.dataset.studentId = table.rows[i].cells[0].getAttribute('data-student-id');
        checkbox.dataset.date = newDate;
        checkbox.dataset.sessionNumber = newSessionNumber;

        cell.appendChild(checkbox);
        table.rows[i].insertBefore(cell, table.rows[i].cells[beforeLastColumnIndex]);
    }
}

// Обработка нажатия на кнопки "Сохранить" и "Удалить" в столбиках
function handleAttendanceActions(event) {
    const target = event.target;

    if (target.classList.contains("save-attendance")) {
        const date = target.dataset.date;
        const sessionNumber = target.dataset.session;
        saveAttendance(date, sessionNumber);
    }

    if (target.classList.contains("delete-attendance-column")) {
        const date = target.dataset.date;
        const sessionNumber = target.dataset.session;
        deleteAttendanceColumn(date, sessionNumber);
    }
}

// Функция сохранения посещаемости
function saveAttendance(date, sessionNumber) {
    const checkboxes = document.querySelectorAll(`input[type='checkbox'][data-date='${date}'][data-session-number='${sessionNumber}']`);
    const attendanceData = [];

    checkboxes.forEach(checkbox => {
        attendanceData.push({
            StudentId: parseInt(checkbox.dataset.studentId),
            Date: date,
            SessionNumber: parseInt(sessionNumber),
            IsPresent: checkbox.checked,
            ClassId: parseInt(document.querySelector('input[name="ClassId"]').value)
        });
    });

    if (attendanceData.length === 0) {
        alert("Нет данных для сохранения.");
        return;
    }

    fetch('/Shared/SaveAttendance', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(attendanceData)
    }).then(response => {
        if (response.ok) {
            alert('Посещаемость успешно сохранена');
            location.reload();
        } else {
            response.text().then(text => {
                alert('Не удалось сохранить посещаемость: ' + text);
            });
        }
    }).catch(error => {
        console.error('Ошибка при сохранении посещаемости:', error);
        alert('Произошла ошибка при сохранении посещаемости.');
    });
}

// Функция удаления колонки посещаемости
async function deleteAttendanceColumn(date, sessionNumber) {
    if (!confirm("Вы уверены, что хотите удалить эту колонку?")) return;

    try {
        const response = await fetch(`/Shared/DeleteAttendanceColumn?date=${encodeURIComponent(date)}&sessionNumber=${encodeURIComponent(sessionNumber)}`, {
            method: 'POST'
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error("Не удалось удалить колонку посещаемости: " + errorText);
        }

        alert("Колонка посещаемости успешно удалена");
        location.reload();
    } catch (error) {
        console.error("Ошибка при удалении колонки посещаемости:", error);
        alert("Произошла ошибка при удалении колонки посещаемости: " + error.message);
    }
}
