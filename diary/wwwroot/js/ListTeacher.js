document.addEventListener("DOMContentLoaded", function () {
    // Поиск преподавателей по клику на кнопку
    document.getElementById("searchTeacherBtn").addEventListener("click", function () {
        var searchTerm = document.getElementById("searchTeacherInput").value;

        // Показать спиннер во время поиска
        document.getElementById("loader").style.display = "block";
        document.querySelector(".container-fluid").style.display = "none";
               
        $.ajax({
            url: '/Admin/FilterTeachers',
            type: 'GET',
            data: { searchTerm: searchTerm },
            success: function (data) {
                $('#teachersTableContainer').html(data);

                // Скрываем спиннер после успешной загрузки
                document.getElementById("loader").style.display = "none";
                document.querySelector(".container-fluid").style.display = "block";
            },
            error: function () {
                alert('Ошибка при поиске преподавателей.');
                document.getElementById("loader").style.display = "none";
                document.querySelector(".container-fluid").style.display = "block";
            }
        });
    });

    // Логика для отображения страницы после загрузки всех изображений
    var container = document.querySelector(".container-fluid");
    var images = container.querySelectorAll("img");
    var totalImages = images.length;
    var loadedImages = 0;

    function checkAllImagesLoaded() {
        loadedImages++;
        if (loadedImages === totalImages) {
            
            document.getElementById("loader").style.display = "none";
            container.style.display = "block";
        }
    }

    // Проверка загрузки каждого изображения
    images.forEach(function (img) {
        if (img.complete) {
            checkAllImagesLoaded();
        } else {
            img.addEventListener("load", checkAllImagesLoaded);
            img.addEventListener("error", checkAllImagesLoaded); 
        }
    });

    // Если изображений нет, сразу показываем контейнер
    if (totalImages === 0) {
        document.getElementById("loader").style.display = "none";
        container.style.display = "block";
    }
});
