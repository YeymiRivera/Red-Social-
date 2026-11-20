
const API_URL = "http://localhost:5290";

const botonGoogle = document.getElementById("googleLogin");
const botonGitHub = document.getElementById("githubLogin");

botonGoogle.addEventListener("click", function () {

    // Redirige al endpoint de autenticación de Google
    window.location.href = `${API_URL}/api/Auth/GoogleLogin`;

});
botonGitHub.addEventListener("click", function () {
window.location.href = `${API_URL}/api/Auth/GithubLogin`;
});
