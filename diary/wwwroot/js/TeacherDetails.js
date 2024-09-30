

function openAssignUserModal(personContactId, contactType) {
    document.getElementById('personContactId').value = personContactId;
    document.getElementById('contactType').value = contactType;
    $('#assignUserModal').modal('show');
}

//Создать и назначить пользователя
function assignUser() {
    const form = document.getElementById('assignUserForm');
    const personContactId = document.getElementById('personContactId').value;
    const userName = document.getElementById('userName').value;
    const password = document.getElementById('password').value;
    const roles = Array.from(form.querySelectorAll('input[name="userRoles"]:checked')).map(cb => cb.value);
    const contactType = document.getElementById('contactType').value; 

    $.ajax({
        url: '/Admin/CreateUser',
        type: 'POST',
        data: {
            personContactId: personContactId,
            userName: userName,
            password: password,
            userRoles: roles,
            contactType: contactType // Отправить contactType
        },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('An error occurred while creating the user.');
        }
    });
}


function deleteUser(userId, personContactId, contactType) {
    $.ajax({
        url: '/Admin/DeleteUser',
        type: 'POST',
        data: {
            personContactId: personContactId,
            contactType: contactType
        },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert('An error occurred while deleting the user.');
        }
    });
}

function openRoleModal(userId) {
    document.getElementById('editUserId').value = userId;
    $('#roleModal').modal('show');

    
    document.querySelectorAll('#editRoles .form-check-input').forEach(input => {
        input.checked = false;
    });

    // Получение текущих ролей пользователя
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

//Обновление ролей пользователя
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

//Генерация пароля для пользователя
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

document.addEventListener('DOMContentLoaded', function () {
    const lessonTypes = {
        'laboratoryworks': 'Лабораторные работы',
        'practicalclasses': 'Практические занятия',
        'seminars': 'Семинары',
        'colloquiums': 'Коллоквиумы',
        'consultations': 'Консультации',
        'lectures': 'Лекции'
    };

    document.querySelectorAll('.lesson-type').forEach(function (element) {
        const type = element.dataset.lessonType;
        console.log(`Type: ${type}`); // Отладочное сообщение
        if (lessonTypes[type]) {
            element.textContent = lessonTypes[type];
        } else {
            console.log(`Unknown type: ${type}`); // Отладочное сообщение для неизвестного типа
        }
    });
});

