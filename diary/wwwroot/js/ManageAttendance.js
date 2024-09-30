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
});

// функция добавления новой колонки без сохранения в системе
function addSessionColumn() {
    const table = document.getElementById("attendanceTable");
    const newDateInput = document.getElementById("newDateInput");
    const newDate = newDateInput.value;

    if (!newDate) {
        alert("Please select a date.");
        return;
    }

    const existingSessionNumbers = Array.from(document.querySelectorAll(`th[data-date='${newDate}']`))
        .map(th => parseInt(th.getAttribute('data-session'), 10));
    const newSessionNumber = existingSessionNumbers.length > 0 ? Math.max(...existingSessionNumbers) + 1 : 1;

    const newHeader = document.createElement("th");
    newHeader.className = "text-center";
    newHeader.style.minWidth = "150px";
    newHeader.style.maxWidth = "250px";
    newHeader.setAttribute('data-date', newDate);
    newHeader.setAttribute('data-session', newSessionNumber);
    newHeader.innerHTML = `
        <div class="session-header">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                ${newDate} / ${newSessionNumber}
            </div>
            <div class="text-warning">не сохранено</div>
            <div style="margin-top: 5px;">
                <button type="button" class="btn btn-sm btn-primary submit-to-group-head" style="width: 100%;" data-date="${newDate}" data-session="${newSessionNumber}">Send to Group Head</button>
            </div>
            <div style="margin-top: 5px;">
                <button type="button" class="btn btn-sm btn-secondary save-attendance" style="width: 100%;" data-date="${newDate}" data-session="${newSessionNumber}">Save</button>
            </div>
        </div>
    `;

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

// Обработка нажатия на кнопку в столбиках
function handleAttendanceActions(event) {
    const target = event.target;
    if (target.classList.contains("save-attendance")) {
        saveAttendance(target.dataset.date, target.dataset.session);
    } else if (target.classList.contains("submit-to-teacher")) {
        submitAttendanceToTeacher(target.dataset.date, target.dataset.session);
    } else if (target.classList.contains("submit-to-group-head")) {
        submitAttendanceToGroupHead(target.dataset.date, target.dataset.session);
    } else if (target.classList.contains("delete-attendance-column")) {
        deleteAttendanceColumn(target.dataset.date, target.dataset.session);
    }
}

function saveAttendance(date, sessionNumber) {
    const checkboxes = document.querySelectorAll(`input[type='checkbox'][data-date='${date}'][data-session-number='${sessionNumber}']`);
    const attendanceData = [];

    checkboxes.forEach(checkbox => {
        attendanceData.push({
            StudentId: checkbox.dataset.studentId,
            Date: date,
            SessionNumber: sessionNumber,
            IsPresent: checkbox.checked,
            ClassGroupId: document.querySelector('input[name="ClassGroupId"]').value
        });
    });

    fetch('/Teacher/SaveAttendance', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(attendanceData)
    }).then(response => {
        if (response.ok) {
            alert('Attendance saved successfully');
            location.reload();
        } else {
            alert('Failed to save attendance');
        }
    }).catch(error => {
        console.error('Error saving attendance:', error);
        alert('An error occurred while saving attendance.');
    });
}

function submitAttendanceToGroupHead(date, sessionNumber) {
    const checkboxes = document.querySelectorAll(`input[type='checkbox'][data-date='${date}'][data-session-number='${sessionNumber}']`);
    const attendanceData = [];

    checkboxes.forEach(checkbox => {
        attendanceData.push({
            StudentId: checkbox.dataset.studentId,
            Date: date,
            SessionNumber: sessionNumber,
            IsPresent: checkbox.checked,
            ClassGroupId: document.querySelector('input[name="ClassGroupId"]').value
        });
    });

    fetch('/Teacher/SubmitAttendanceToGroupHead', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(attendanceData)
    }).then(response => {
        if (response.ok) {
            alert('Attendance submitted to Group Head successfully');
            location.reload(); // Reload to reflect changes
        } else {
            alert('Failed to submit attendance');
        }
    }).catch(error => {
        console.error('Error submitting attendance:', error);
        alert('An error occurred while submitting attendance.');
    });
}

// Функция для отправки колонки посещаемости преподавателю
function submitAttendanceToTeacher(date, sessionNumber) {
    const checkboxes = document.querySelectorAll(`input[type='checkbox'][data-date='${date}'][data-session-number='${sessionNumber}']`);
    const attendanceData = [];

    checkboxes.forEach(checkbox => {
        attendanceData.push({
            StudentId: checkbox.dataset.studentId,
            Date: date,
            SessionNumber: sessionNumber,
            IsPresent: checkbox.checked,
            ClassGroupId: document.querySelector('input[name="ClassGroupId"]').value
        });
    });

    fetch('/GroupHead/SubmitAttendanceToTeacher', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(attendanceData)
    }).then(response => {
        if (response.ok) {
            alert('Attendance sent to teacher successfully');
            location.reload(); // Reload to reflect changes
        } else {
            alert('Failed to send attendance');
        }
    }).catch(error => {
        console.error('Error sending attendance:', error);
        alert('An error occurred while sending attendance.');
    });
}

function deleteAttendanceColumn(date, sessionNumber) {
    if (!confirm('Are you sure you want to delete this column?')) return;

    fetch(`/Teacher/DeleteAttendanceColumn?date=${date}&sessionNumber=${sessionNumber}`, {
        method: 'POST'
    }).then(response => {
        if (response.ok) {
            alert('Attendance column deleted successfully');
            location.reload(); // Refresh to update the view
        } else {
            alert('Failed to delete attendance column');
        }
    }).catch(error => {
        console.error('Error deleting attendance column:', error);
        alert('An error occurred while deleting the attendance column.');
    });
}
