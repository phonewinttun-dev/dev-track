const menuToggle = document.getElementById("main-menu-toggle");
const mainMenu = document.getElementById("main-menu");

if (menuToggle && mainMenu) {
    function setMenuOpen(isOpen) {
        menuToggle.setAttribute("aria-expanded", String(isOpen));
        mainMenu.classList.toggle("hidden", !isOpen);
    }

    menuToggle.addEventListener("click", () => {
        setMenuOpen(menuToggle.getAttribute("aria-expanded") !== "true");
    });

    menuToggle.closest("nav").addEventListener("keydown", event => {
        if (event.key === "Escape" && menuToggle.getAttribute("aria-expanded") === "true") {
            setMenuOpen(false);
            menuToggle.focus();
        }
    });

    window.matchMedia("(min-width: 1024px)").addEventListener("change", () => setMenuOpen(false));
}
