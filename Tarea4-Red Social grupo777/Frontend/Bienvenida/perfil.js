
// Datos de ejemplo devueltos por tu API
const usuario = {
    id: 1,
    user: "Yeymi miguel Rivera Castillo",
    email: "yeymirivera13@gmail.com",
    biografia: "",
    fecha: "2026-11-19T02:35:50.5609355Z",
    estado: "Activo"
};

// Guardamos una copia para poder restablecer los campos
const usuarioOriginal = { ...usuario };

// Referencias a los elementos HTML
const nombre = document.getElementById("nombre");
const email = document.getElementById("email");
const biografia = document.getElementById("biografia");
const idUsuario = document.getElementById("idUsuario");
const fecha = document.getElementById("fecha");

const mensaje = document.getElementById("mensaje");

// Mostrar información
function mostrarUsuario(datos) {
    nombre.value = datos.user;
    email.value = datos.email;
    biografia.value = datos.biografia;

    fecha.value = new Date(datos.fecha).toLocaleString("es-BO");

    document.getElementById("nombreTitulo").textContent = datos.user;

    document.getElementById("avatar").textContent =
        datos.user.charAt(0).toUpperCase();

    document.getElementById("estado").textContent = datos.estado;
}

// Cargar los datos al abrir la página
mostrarUsuario(usuario);

// Simular el guardado
document.getElementById("formPerfil").addEventListener("submit", function(event) {

    event.preventDefault();

    mensaje.textContent =
        "Vista previa: los cambios todavía no se guardaron en el servidor.";

});

// Restablecer los campos
document.getElementById("restablecer").addEventListener("click", function() {

    mostrarUsuario(usuarioOriginal);

    mensaje.textContent = "Se restauraron los datos originales.";

});

// Botón de salida (solo visual por ahora)
function cerrarVista() {
    alert("La función de cerrar sesión todavía no está implementada.");
}
