
// Слайдер
document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("toggleButton");
    const closeButton = document.getElementById("closeButton");
    const sidebar = document.getElementById("sidebar");

    toggleButton.addEventListener("click", function () {
        sidebar.classList.add("active");
        toggleButton.style.display = "none";
        closeButton.style.display = "block";
    });

    closeButton.addEventListener("click", function () {
        sidebar.classList.remove("active");
        closeButton.style.display = "none";
        toggleButton.style.display = "block";
    });


    let touchStartX = 0;
    let touchEndX = 0;

    function handleSwipe() {
        if (touchEndX < touchStartX) {
            sidebar.classList.remove("active");
            closeButton.style.display = "none";
            toggleButton.style.display = "block";
        }
    }

    document.addEventListener("touchstart", function (event) {
        touchStartX = event.changedTouches[0].screenX;
    });

    document.addEventListener("touchend", function (event) {
        touchEndX = event.changedTouches[0].screenX;
        handleSwipe();
    });
});