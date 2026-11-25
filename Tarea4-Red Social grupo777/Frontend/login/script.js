
const API_URL = (window.APP_CONFIG && window.APP_CONFIG.API_URL) || "http://localhost:5290";
const alerta = document.getElementById("alerta");
const opcionesLogin = document.getElementById("opcionesLogin");
const perfil = document.getElementById("perfil");

// Conservamos la solicitud original de OpenIddict.
const parametros = new URLSearchParams(window.location.search);
const returnUrl = parametros.get("returnUrl");

// Construye las direcciones de los proveedores.
function crearUrlLogin(endpoint) {
    const url = new URL(endpoint, API_URL);

    if (returnUrl) {
        url.searchParams.set("returnUrl", returnUrl);
    }

    return url.toString();
}

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

// Redirige al proveedor de autenticación.
function iniciarLogin(boton, endpoint, proveedor) {
    ponerCargando(boton, `Conectando con ${proveedor}...`);
    window.location.href = crearUrlLogin(endpoint);
}

document.addEventListener("DOMContentLoaded", () => {
    // Si la página se abrió sin una solicitud de OpenIddict (sin returnUrl) no habrá
    // access token al terminar. Si estamos en el frontend (Live Server / Docker),
    // volvemos a index.html, que inicia el flujo correcto.
    if (!returnUrl && window.location.origin !== new URL(API_URL).origin) {
        window.location.replace(new URL("../index.html", window.location.href).href);
        return;
    }

    if (!returnUrl) {
        mostrarError(
            "Abre index.html (Live Server) y pulsa «Iniciar sesión» para obtener un access token."
        );
    }

    const error = parametros.get("error");

    if (error) {
        mostrarError(error);
    }

    const dropbox = document.getElementById("dropboxLogin");
    const figma = document.getElementById("figmaLogin");
    const twitter = document.getElementById("twitterLogin");
    const tiktok = document.getElementById("tiktokLogin");
    const discord = document.getElementById("discordLogin");
    const slack = document.getElementById("slackLogin");

    if (dropbox) dropbox.href = `${API_URL}/api/Auth/dropbox/login`;
    if (figma) figma.href = `${API_URL}/api/Auth/figma/login`;
    if (twitter) twitter.href = `${API_URL}/api/Auth/twitter/login`;
    if (tiktok) tiktok.href = `${API_URL}/api/Auth/tiktok/login`;
    if (discord) discord.href = `${API_URL}/api/Auth/discord/login`;
    if (slack) slack.href = `${API_URL}/api/Auth/slack/login`;

    const google = document.getElementById("googleLogin");
    const github = document.getElementById("githubLogin");

    // Configuramos los enlaces de los proveedores.
    if (dropbox) {
        dropbox.href = crearUrlLogin("/api/Auth/dropbox/login");

        dropbox.addEventListener("click", () => {
            ponerCargando(dropbox, "Conectando con Dropbox...");
        });
    }

    if (figma) {
        figma.href = crearUrlLogin("/api/Auth/figma/login");

        figma.addEventListener("click", () => {
            ponerCargando(figma, "Conectando con Figma...");
        });
    }

    discord?.addEventListener("click", () => {
        ponerCargando(discord, "Conectando con Discord...");
    });

    slack?.addEventListener("click", () => {
        ponerCargando(slack, "Conectando con Slack...");
    });

    document.getElementById("continuar")?.addEventListener("click", () => {
        window.location.href = `${API_URL}/Bienvenida/bienvenida.html`;
    });

        twitter.addEventListener("click", () => {
            ponerCargando(twitter, "Conectando con X...");
        });
    }

    if (tiktok) {
        tiktok.href = crearUrlLogin("/api/Auth/tiktok/login");

        tiktok.addEventListener("click", () => {
            ponerCargando(tiktok, "Conectando con TikTok...");
        });
    }

    // Google.
    google?.addEventListener("click", (evento) => {
        evento.preventDefault();

        iniciarLogin(
            google,
            "/api/Auth/GoogleLogin",
            "Google"
        );
    });

    // GitHub.
    github?.addEventListener("click", (evento) => {
        evento.preventDefault();

        iniciarLogin(
            github,
            "/api/Auth/GithubLogin",
            "GitHub"
        );
    });

    // Cerrar sesión local.
    document.getElementById("logout")?.addEventListener("click", async (evento) => {
        const boton = evento.currentTarget;

        ponerCargando(boton, "Cerrando sesión...");

        try {
            await fetch(`${API_URL}/api/Auth/logout`, {
                method: "POST",
                credentials: "include"
            });
        } catch (error) {
            console.error("Error al cerrar sesión:", error);
        } finally {
            window.location.href = `${API_URL}/login.html`;
        }
    });

});
