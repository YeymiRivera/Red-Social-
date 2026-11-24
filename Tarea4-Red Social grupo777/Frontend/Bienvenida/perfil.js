const API_URL = (() => {
    if (window.API_URL) return window.API_URL;

    const { protocol, hostname, port } = window.location;

    if (port === "5500") {
        return `${protocol}//${hostname}:5290`;
    }

    return window.location.origin;
})();

const nombre = document.getElementById("nombre");
const email = document.getElementById("email");
const biografia = document.getElementById("biografia");
const fecha = document.getElementById("fecha");
const mensaje = document.getElementById("mensaje");

let usuarioOriginal = {
    user: "",
    email: "",
    biografia: "",
    fecha: "",
    estado: "Activo"
};

function mostrarUsuario(datos) {
    nombre.value = datos.user || "";
    email.value = datos.email || "";
    biografia.value = datos.biografia || "";

    fecha.value = datos.fecha
        ? new Date(datos.fecha).toLocaleString("es-BO")
        : "—";

    document.getElementById("nombreTitulo").textContent =
        datos.user || "Mi perfil";

    document.getElementById("avatar").textContent =
        (datos.user || "U").charAt(0).toUpperCase();

    document.getElementById("estado").textContent =
        datos.estado || "Activo";
}

async function cargarPerfil() {
    try {
        const respuesta = await fetch(`${API_URL}/api/Auth/me`, {
            credentials: "include"
        });

        if (!respuesta.ok) {
            window.location.href = `${API_URL}/login.html`;
            return;
        }

        const sesion = await respuesta.json();

        let datos = {
            user: sesion.nombre || "Usuario",
            email: sesion.email || "",
            biografia: "",
            fecha: "",
            estado: "Activo"
        };

        try {
            const perfil = await fetch(
                `${API_URL}/api/Usuario/Buscar/${encodeURIComponent(sesion.nombre)}`,
                { credentials: "include" });

            if (perfil.ok) {
                const usuario = await perfil.json();

                datos = {
                    user: usuario.user ?? datos.user,
                    email: usuario.email ?? datos.email,
                    biografia: usuario.biografia ?? datos.biografia,
                    fecha: usuario.fecha ?? datos.fecha,
                    estado: usuario.estado ?? datos.estado
                };
            }
        } catch {
            // Si falla la consulta del perfil, se muestran los datos de la sesión.
        }

        usuarioOriginal = { ...datos };
        mostrarUsuario(datos);
    } catch {
        window.location.href = `${API_URL}/login.html`;
    }
}

document.addEventListener("DOMContentLoaded", () => {
    cargarPerfil();

    document.getElementById("formPerfil")?.addEventListener("submit", (event) => {
        event.preventDefault();
        mensaje.textContent =
            "Vista previa: los cambios todavía no se guardaron en el servidor.";
    });

    document.getElementById("restablecer")?.addEventListener("click", () => {
        mostrarUsuario(usuarioOriginal);
        mensaje.textContent = "Se restauraron los datos originales.";
    });
});

async function cerrarVista() {
    try {
        await fetch(`${API_URL}/api/Auth/logout`, {
            method: "POST",
            credentials: "include"
        });
    } finally {
        window.location.href = `${API_URL}/login.html`;
    }
}
