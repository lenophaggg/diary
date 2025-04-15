function openAddStudentModal() {
    $('#addStudentModal').modal('show');
}

function addStudent() {
    const form = document.getElementById('addStudentForm');
    const studentName = document.getElementById('studentName').value;
    const universityStudentId = document.getElementById('universityStudentId').value;
    const groupNumber = document.getElementById('groupNumber').textContent.trim().split(' ').pop();

    $.ajax({
        url: '/GroupHead/AddStudent',
        type: 'POST',
        data: {
            studentName: studentName,
            universityStudentId: universityStudentId,
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
            alert('An error occurred while adding the student.');
        }
    });
}






function removeStudent(studentId) {
    $.ajax({
        url: '/GroupHead/RemoveStudent',
        type: 'POST',

        data: {
            studentId: studentId
        },

        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert('Ошибка при удалении студента');
            }
        },
        error: function () {
            alert('Ошибка при выполнении запроса');
        }
    });
}

// Новые функции
function clearGroup() {
    if (confirm('Вы уверены, что хотите очистить группу? Все студенты будут отвязаны от группы.')) {
        const groupNumber = document.getElementById('groupNumber').textContent.trim().split(' ').pop();

        $.ajax({
            url: '/GroupHead/ClearGroup',
            type: 'POST',
            data: {
                groupNumber: groupNumber
            },
            success: function (response) {
                if (response.success) {
                    document.getElementById('groupUpdateStatus').textContent = 'Группа очищена';
                    setTimeout(() => location.reload(), 1000);
                } else {
                    alert(response.message || 'Ошибка при очистке группы');
                }
            },
            error: function () {
                alert('Ошибка при выполнении запроса');
            }
        });
    }
}

function moveGroup() {
    const newGroupNumber = document.getElementById('newGroupNumber').value.trim();
    const currentGroupNumber = document.getElementById('groupNumber').textContent.trim().split(' ').pop();

    if (!newGroupNumber) {
        alert('Пожалуйста, укажите номер новой группы');
        return;
    }

    if (newGroupNumber === currentGroupNumber) {
        alert('Новая группа должна отличаться от текущей');
        return;
    }

    if (confirm('Вы уверены, что хотите перенести всех студентов в группу ' + newGroupNumber + '?')) {
        $.ajax({
            url: '/GroupHead/MoveGroup',
            type: 'POST',
            data: {
                currentGroupNumber: currentGroupNumber,
                newGroupNumber: newGroupNumber
            },
            success: function (response) {
                if (response.success) {
                    document.getElementById('groupUpdateStatus').textContent = 'Группа перенесена';
                    setTimeout(() => location.reload(), 1000);
                } else {
                    alert(response.message || 'Ошибка при переносе группы');
                }
            },
            error: function () {
                alert('Ошибка при выполнении запроса');
            }
        });
    }
}