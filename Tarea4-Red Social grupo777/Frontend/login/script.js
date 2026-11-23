const API_URL = "http://localhost:5290";

document.addEventListener("DOMContentLoaded", () => {
    const botonGoogle = document.getElementById("googleLogin");
    const botonGitHub = document.getElementById("githubLogin");

    botonGoogle?.addEventListener("click", () => {
        window.location.href = `${API_URL}/api/Auth/GoogleLogin`;
    });

    botonGitHub?.addEventListener("click", () => {
        window.location.href = `${API_URL}/api/Auth/GithubLogin`;
    });
});

async function cargarPerfil() {
    const res = await fetch(`${API_URL}/api/Auth/me`, {
        credentials: "include"
    });

    const perfil = document.getElementById("perfil");

    if (res.ok) {
        const data = await res.json();

        perfil.innerHTML =
            `<strong>Estado:</strong> Autenticado<br>` +
            `<strong>Proveedor:</strong> ${data.proveedor}<br>` +
            `<strong>ID Usuario:</strong> ${data.id}<br>` +
            `<strong>Nombre:</strong> ${data.nombre}<br>` +
            `<strong>Correo:</strong> ${data.email}`;
    } else {
        perfil.innerHTML = `<strong>Estado:</strong> Sin sesión activa`;
    }
}

async function cerrarSesion() {
    await fetch(`${API_URL}/api/Auth/logout`, {
        method: "POST",
        credentials: "include"
    });

    cargarPerfil();
}