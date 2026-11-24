// Si la página la sirve el frontend (contenedor Nginx en 5500), la API vive
// en el puerto 5290. Si la sirve el propio backend, usamos su mismo origen.
const API_URL = (() => {
    if (window.API_URL) return window.API_URL;

    const { protocol, hostname, port } = window.location;

    if (port === "5500") {
        return `${protocol}//${hostname}:5290`;
    }

    return window.location.origin;
})();

const alerta = document.getElementById("alerta");
const opcionesLogin = document.getElementById("opcionesLogin");
const perfil = document.getElementById("perfil");

function mostrarError(mensaje) {
    if (!alerta) return;
    alerta.textContent = mensaje;
    alerta.hidden = false;
}

function ponerCargando(boton, texto) {
    if (!boton) return;
    boton.disabled = true;
    const icono = boton.querySelector(".btn-icon");
    if (icono) {
        icono.innerHTML = '<span class="spinner"></span>';
    }
    const etiqueta = boton.querySelector(".btn-text");
    if (etiqueta) {
        etiqueta.textContent = texto;
    }
}

document.addEventListener("DOMContentLoaded", () => {
    const parametros = new URLSearchParams(window.location.search);
    const error = parametros.get("error");

    if (error) {
        mostrarError(error);
        window.history.replaceState({}, "", window.location.pathname);
    }

    const dropbox = document.getElementById("dropboxLogin");
    const figma = document.getElementById("figmaLogin");
    const twitter = document.getElementById("twitterLogin");
    const tiktok = document.getElementById("tiktokLogin");

    if (dropbox) dropbox.href = `${API_URL}/api/Auth/dropbox/login`;
    if (figma) figma.href = `${API_URL}/api/Auth/figma/login`;
    if (twitter) twitter.href = `${API_URL}/api/Auth/twitter/login`;
    if (tiktok) tiktok.href = `${API_URL}/api/Auth/tiktok/login`;

    const google = document.getElementById("googleLogin");
    const github = document.getElementById("githubLogin");

    google?.addEventListener("click", () => {
        ponerCargando(google, "Conectando con Google...");
        window.location.href = `${API_URL}/api/Auth/GoogleLogin`;
    });

    github?.addEventListener("click", () => {
        ponerCargando(github, "Conectando con GitHub...");
        window.location.href = `${API_URL}/api/Auth/GithubLogin`;
    });

    dropbox?.addEventListener("click", () => {
        ponerCargando(dropbox, "Conectando con Dropbox...");
    });

    figma?.addEventListener("click", () => {
        ponerCargando(figma, "Conectando con Figma...");
    });

    twitter?.addEventListener("click", () => {
        ponerCargando(twitter, "Conectando con X...");
    });

    tiktok?.addEventListener("click", () => {
        ponerCargando(tiktok, "Conectando con TikTok...");
    });

    document.getElementById("continuar")?.addEventListener("click", () => {
        window.location.href = `${API_URL}/Bienvenida/bienvenida.html`;
    });

    document.getElementById("logout")?.addEventListener("click", async () => {
        await fetch(`${API_URL}/api/Auth/logout`, {
            method: "POST",
            credentials: "include"
        });

        window.location.href = `${API_URL}/login.html`;
    });

    cargarPerfil();
});

async function cargarPerfil() {
    try {
        const respuesta = await fetch(`${API_URL}/api/Auth/me`, {
            credentials: "include"
        });

        if (!respuesta.ok) return;

        const datos = await respuesta.json();

        if (!datos.autenticado) return;

        opcionesLogin.hidden = true;
        perfil.hidden = false;

        const nombre = datos.nombre || "Usuario";
        const email = datos.email || "Sin correo";
        const proveedor = datos.proveedor || "Sesión";

        document.getElementById("perfilNombre").textContent = nombre;
        document.getElementById("perfilEmail").textContent = email;
        document.getElementById("perfilProveedor").textContent = proveedor;
        document.getElementById("perfilAvatar").textContent =
            nombre.charAt(0).toUpperCase();
    } catch {
        // Si la API no responde, se dejan visibles los botones de login.
    }
}
