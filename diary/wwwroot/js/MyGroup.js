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
