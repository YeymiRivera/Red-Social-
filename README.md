# Red Social

API REST desarrollada con ASP.NET Core .NET 10 y PostgreSQL. Permite gestionar usuarios mediante operaciones CRUD, con persistencia de datos en una base de datos PostgreSQL.

## Ejecutar en local con `dotnet run`

Requisitos: .NET 10 y PostgreSQL escuchando en el puerto **5433** con la base `RedsocialDB`, usuario `postgres` y contraseña `11ye` (así lo espera `appsettings.Development.json`).

Si no tienes esa base, puedes crearla con Docker:

```bash
docker run -d --name pg-gerson \
  -e POSTGRES_PASSWORD=11ye \
  -e POSTGRES_DB=RedsocialDB \
  -p 5433:5432 postgres:17
```

Luego ejecuta:

```bash
cd "Tarea4-Red Social grupo777"
dotnet run
```

Abre <http://localhost:5290/login.html>.

Si aparece el error `Failed to bind to address http://127.0.0.1:5290: address already in use`, hay otro proceso usando el puerto. Libéralo con:

```bash
fuser -k 5290/tcp
```

## Ejecutar con Docker (Construcción)

Abre una terminal en la raíz del proyecto, donde se encuentran el archivo `contructor.yml` y `.env.example`.

Primero, crea tu archivo de variables de entorno:

```bash
cp .env.example .env
```

Configura las credenciales y el puerto en `.env`:

```env
BACKEND_PORT=5290
FRONTEND_PORT=5500
POSTGRES_DB=RedsocialDB
POSTGRES_USER=postgres
POSTGRES_PASSWORD=11ye
```

> El proyecto también funciona sin `.env`: los archivos Compose usan esos valores por defecto. Mantén `BACKEND_PORT=5290` para que coincidan las URLs de callback de los proveedores OAuth.

Construye la imagen y levanta los servicios:

```bash
docker compose -f contructor.yml up -d --build
```

Comprueba que ambos contenedores estén funcionando:

```bash
docker compose -f contructor.yml ps
```

La configuración levanta dos servicios:

- **Backend:** API desarrollada en ASP.NET Core.
- **PostgreSQL:** base de datos que almacena la información de los usuarios.

La API se conecta a PostgreSQL utilizando el nombre del servicio `postgres` como host. Los datos se conservan en el volumen nombrado `datos-postgres`, incluso si se detienen o eliminan los contenedores.

## Consultar la API

Una vez iniciados los servicios, puedes acceder a Scalar desde el navegador:

```text
http://localhost:5290/scalar
```

También puedes consultar la especificación OpenAPI:

```text
http://localhost:5290/openapi/v1.json
```

Si modificas `BACKEND_PORT` en `.env`, utiliza el nuevo puerto en las direcciones anteriores. Para el login con proveedores externos se recomienda mantener `5290`, porque las URLs de callback registradas en GitHub, Dropbox y Figma apuntan a ese puerto.

## Inicio de sesión

La interfaz rediseñada está en:

```text
Tarea4-Red Social grupo777/Frontend/login/login.html
Tarea4-Red Social grupo777/Frontend/login/login.css
Tarea4-Red Social grupo777/Frontend/login/script.js
```

El perfil está en `Tarea4-Red Social grupo777/Frontend/Bienvenida/`.

Puedes abrir el login desde:

- Backend/Nginx: <http://localhost:5290/login.html>
- Contenedor del frontend: <http://localhost:5500/login/login.html>

GitHub, Dropbox, Figma y X (Twitter) ya están configurados en `appsettings.Development.json` (ejecución local) y `appsettings.Production.json` (Docker). Google tiene el **Client ID** configurado, pero el **Client Secret** no se guarda en el repositorio porque GitHub bloquea los secretos publicados.

Las URLs de callback registradas en los portales de los proveedores son:

```text
http://localhost:5290/signin-google
http://localhost:5290/auth/twitter/callback
```

### Google

El Client Secret se configura una sola vez en cada máquina, fuera de Git:

**Ejecución local con `dotnet run`:**

```bash
cd "Tarea4-Red Social grupo777"
dotnet user-secrets set "Google:ClientSecret" "TU_CLIENT_SECRET"
```

**Docker:**

```bash
cd /home/gerson/Red-Social-
printf '\nGOOGLE_CLIENT_SECRET=TU_CLIENT_SECRET\n' >> .env
docker compose -f contructor.yml up -d --build
```

El archivo `.env` está en `.gitignore`, así que el secreto no se sube.

En <https://console.cloud.google.com/apis/credentials> verifica que el URI de redirección autorizado sea `http://localhost:5290/signin-google` y que tu correo esté como usuario de prueba si la pantalla de consentimiento está en modo de prueba.

## Arrancar desde la imagen local (Despliegue)

Después de construir la imagen `redsocial`, puedes utilizar `despliegue.yml` para levantar los servicios sin volver a construir el backend.

Ejecuta:

```bash
docker compose -f despliegue.yml up -d
```

Este archivo utiliza la imagen local previamente construida mediante `contructor.yml`. No necesita el código fuente ni el Dockerfile para iniciar el backend.

Para comprobar los contenedores:

```bash
docker compose -f despliegue.yml ps
```

## Publicar la imagen en Docker Hub

Si deseas utilizar la imagen en otro equipo, primero debes etiquetarla con tu usuario de Docker Hub:

```bash
docker tag redsocial TU_USUARIO/redsocial:1.0
```

Inicia sesión:

```bash
docker login
```

Publica la imagen:

```bash
docker push TU_USUARIO/redsocial:1.0
```

En el archivo `despliegue.yml`, utiliza la imagen publicada:

```yaml
backend:
  image: TU_USUARIO/redsocial:1.0
  pull_policy: always
```

De esta manera, Docker podrá descargar la imagen desde Docker Hub al momento del despliegue.

## Detener los servicios

Para detener y eliminar los contenedores y la red del proyecto, ejecuta:

```bash
docker compose -f contructor.yml down
```

Los datos de PostgreSQL se conservan porque están almacenados en un volumen nombrado.

**Importante:** no utilices `down -v` si deseas conservar la información de la base de datos.

## Archivos y credenciales

El proyecto utiliza los siguientes archivos:

- `contructor.yml`: configuración para construir la imagen y levantar los servicios.
- `despliegue.yml`: configuración para ejecutar una imagen previamente construida.
- `.env`: contiene las credenciales y variables de entorno utilizadas por Docker Compose.
- `.env.example`: plantilla de las variables necesarias para ejecutar el proyecto.
- `Dockerfile`: instrucciones para construir la imagen del backend.

El archivo `.env` está excluido de Git y Docker mediante `.gitignore` y `.dockerignore`. Únicamente se comparte `.env.example`, para que otros usuarios puedan configurar sus propias credenciales.

## Verificación

Para comprobar el funcionamiento del proyecto:

1. Construir la imagen del backend.
2. Verificar que los contenedores de la API y PostgreSQL estén activos.
3. Acceder a Scalar y consultar los endpoints.
4. Probar las operaciones CRUD de usuarios.
5. Reiniciar los contenedores y comprobar que los datos continúan almacenados en PostgreSQL.
6. Verificar el despliegue utilizando la imagen previamente construida.

##Tablas

Yeymi Miguel Rivera Castillo
Usuarios
Seguidores
Usuarios/Publicaciones
Usuario/sesion
##Metodos de sesion
Yeymi Miguel Rivera Castillo
Google
Github


