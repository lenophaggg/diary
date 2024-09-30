// Скрипт для отображения загрузки до загрузки всех <td> элементов со статусами
document.addEventListener("DOMContentLoaded", function () {
    var container = document.querySelector(".container-fluid");
    var statusElements = document.querySelectorAll("td > span");  // Находим все <span> со статусами
    var totalStatuses = statusElements.length;
    var loadedStatuses = 0;

    // Функция проверки загрузки статусов
    function checkAllStatusesLoaded() {
        loadedStatuses++;
        if (loadedStatuses === totalStatuses) {
            // Скрываем индикатор загрузки и показываем основной контент
            document.getElementById("loader").style.display = "none";
            container.style.display = "block";
        }
    }

    // Проверяем, если все статусы отрендерились
    statusElements.forEach(function (status) {
        if (status.innerHTML.trim() !== "") {
            checkAllStatusesLoaded();
        } else {
            // Если статус пустой, проверяем когда он загрузится
            var observer = new MutationObserver(function (mutations) {
                mutations.forEach(function (mutation) {
                    if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                        checkAllStatusesLoaded();
                        observer.disconnect();  // Останавливаем наблюдателя
                    }
                });
            });

            observer.observe(status, { childList: true });
        }
    });

    // Если нет статусов, сразу показываем контент
    if (totalStatuses === 0) {
        document.getElementById("loader").style.display = "none";
        container.style.display = "block";
    }
});


function createStudentAbsenceRequestForm() {
    const form = document.getElementById('createStudentAbsenceRequestForm');
    const formData = new FormData(form);

    $.ajax({
        url: form.action,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function(response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function() {
            alert('An error occurred while creating the student absence request.');
        }
    });
}