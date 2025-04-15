function deleteGroup(groupNumber) {
    if (confirm(`Вы уверены, что хотите удалить группу ${groupNumber}?`)) {
        $.ajax({
            url: '/Schedule/DeleteGroup',
            type: 'POST',
            data: {
                groupNumber: groupNumber
            },
            success: function (response) {
                if (response.success) {
                    location.reload(); // Перезагрузка страницы после успешного удаления
                } else {
                    alert(response.message || 'Ошибка при удалении группы');
                }
            },
            error: function () {
                alert('Ошибка при выполнении запроса');
            }
        });
    }
}