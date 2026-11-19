
const API_URL = "http://localhost:5290";

const botonGoogle = document.getElementById("googleLogin");

botonGoogle.addEventListener("click", function () {

    // Redirige al endpoint de autenticación de Google
    window.location.href = `${API_URL}/api/Auth/GoogleLogin`;

});
