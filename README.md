# Red Social

API REST desarrollada con ASP.NET Core .NET 10 y PostgreSQL. Permite gestionar usuarios mediante operaciones CRUD, con persistencia de datos en una base de datos PostgreSQL.

## Ejecutar con Docker (Construcción)

Abre una terminal en la raíz del proyecto, donde se encuentran el archivo `contructor.yml` y `.env.example`.

Primero, crea tu archivo de variables de entorno:

```bash
cp .env.example .env
```

Configura las credenciales y el puerto en `.env`:

```env
BACKEND_PORT=8080
POSTGRES_DB=RedSocialDB
POSTGRES_USER=postgres
POSTGRES_PASSWORD=tu_clave
```

Construye la imagen y levanta los servicios:

```bash
docker compose --env-file .env -f contructor.yml up -d --build
```

Comprueba que ambos contenedores estén funcionando:

```bash
docker compose --env-file .env -f contructor.yml ps
```

La configuración levanta dos servicios:

- **Backend:** API desarrollada en ASP.NET Core.
- **PostgreSQL:** base de datos que almacena la información de los usuarios.

La API se conecta a PostgreSQL utilizando el nombre del servicio `postgres` como host. Los datos se conservan en el volumen nombrado `datos-postgres`, incluso si se detienen o eliminan los contenedores.

## Consultar la API

Una vez iniciados los servicios, puedes acceder a Scalar desde el navegador:

```text
http://localhost:8080/scalar
```

También puedes consultar la especificación OpenAPI:

```text
http://localhost:8080/openapi/v1.json
```

Si modificas `BACKEND_PORT` en `.env`, utiliza el nuevo puerto en las direcciones anteriores.

## Arrancar desde la imagen local (Despliegue)

Después de construir la imagen `redsocial`, puedes utilizar `despliegue.yml` para levantar los servicios sin volver a construir el backend.

Ejecuta:

```bash
docker compose --env-file .env -f despliegue.yml up -d
```

Este archivo utiliza la imagen local previamente construida mediante `contructor.yml`. No necesita el código fuente ni el Dockerfile para iniciar el backend.

Para comprobar los contenedores:

```bash
docker compose --env-file .env -f despliegue.yml ps
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
docker compose --env-file .env -f contructor.yml down
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


