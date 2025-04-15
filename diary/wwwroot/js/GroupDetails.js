function openAssignUserModal(personContactId, contactType) {
    document.getElementById('personContactId').value = personContactId;
    document.getElementById('contactType').value = contactType;
    $('#assignUserModal').modal('show');
}

function openAssignGroupHeadModal() {
    $('#assignGroupHeadModal').modal('show');
}

//Создание старосты для группы
function assignGroupHead() {
    const form = document.getElementById('assignGroupHeadForm');
    const studentId = document.getElementById('studentId').value;

    $.ajax({
        url: '/Admin/AssignGroupHead',
        type: 'POST',
        data: {
            studentId: studentId
        },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('An error occurred while assigning the group head.');
        }
    });
}

function openAddStudentModal() {
    $('#addStudentModal').modal('show');
}

function addStudent() {
    const form = document.getElementById('addStudentForm');
    const studentName = document.getElementById('studentName').value;
    const universityStudentId = document.getElementById('universityStudentId').value;
    const groupNumber = document.getElementById('groupNumber').textContent.trim().split(' ').pop();

    $.ajax({
        url: '/Admin/AddStudent',
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

function openRoleModal(userId) {
    document.getElementById('editUserId').value = userId;
    $('#roleModal').modal('show');

    // Clear previous roles selection
    document.querySelectorAll('#editRoles .form-check-input').forEach(input => {
        input.checked = false;
    });

    // Fetch current roles for the user
    $.ajax({
        url: '/Admin/GetUserRoles',
        type: 'GET',
        data: { userId: userId },
        success: function (roles) {
            roles.forEach(role => {
                const roleInput = document.getElementById('editRole_' + role);
                if (roleInput) {
                    roleInput.checked = true;
                }
            });
        },
        error: function () {
            alert('An error occurred while fetching user roles.');
        }
    });
}

function updateRoles() {
    const userId = document.getElementById('editUserId').value;
    const roles = Array.from(document.querySelectorAll('#editRoles input[name="editRoles"]:checked')).map(cb => cb.value);

    $.ajax({
        url: '/Admin/UpdateUserRoles',
        type: 'POST',
        data: {
            userId: userId,
            roles: roles
        },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('An error occurred while updating the roles.');
        }
    });
}

function generatePassword() {
    const length = 8;
    const charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+";
    let password = "";
    let hasUpper = false;
    let hasDigit = false;
    let hasSpecial = false;

    while (password.length < length || !hasUpper || !hasDigit || !hasSpecial) {
        const char = charset[Math.floor(Math.random() * charset.length)];
        password += char;

        if (char >= 'A' && char <= 'Z') hasUpper = true;
        if (char >= '0' && char <= '9') hasDigit = true;
        if ("!@#$%^&*()_+".includes(char)) hasSpecial = true;
    }

    document.getElementById("password").value = password;
}

function removeGroupHead(studentId) {
    $.ajax({
        url: '/Admin/RemoveGroupHead',
        type: 'POST',
        data: {
            studentId: studentId
        },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('An error occurred while removing the group head.');
        }
    });
}

function removeStudent(studentId) {
    $.ajax({
        url: '/Shared/RemoveStudent',
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

// Очистка группы
function clearGroup() {
    if (confirm('Вы уверены, что хотите очистить группу? Все студенты будут отвязаны от группы.')) {
        const groupNumber = document.getElementById('groupNumber').textContent.trim().split(' ').pop();

        $.ajax({
            url: '/Admin/ClearGroup',
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

// Перенос группы
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
            url: '/Admin/MoveGroup',
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