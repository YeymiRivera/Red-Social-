# Red Social

Aplicación web de red social desarrollada con **ASP.NET Core .NET 10**, **PostgreSQL**, **Redis** y un frontend web. El sistema permite gestionar usuarios, autenticación, sesiones y funcionalidades de interacción social.

La aplicación está preparada para ejecutarse mediante Docker Compose, utilizando imágenes Docker del backend y frontend publicadas en Docker Hub. También incorpora Nginx como balanceador de carga para distribuir las solicitudes entre varias instancias del backend.

## 1. Requisitos

Antes de iniciar el proyecto, asegúrate de tener instalado:

- Docker.
- Docker Compose (incluido en Docker Desktop o en Docker Engine con el plugin Compose).
- Una cuenta de Docker Hub, si deseas publicar tus propias imágenes.

## 2. Configuración del archivo `.env`

Antes de ejecutar cualquiera de los archivos Compose, debes crear el archivo `.env` en la **raíz del proyecto**, en el mismo nivel que `contructor.yml` y `despliegue.yml`.

La estructura debe quedar de esta manera:

```text
Red-Social/
├── .env
├── .env.example
├── contructor.yml
├── despliegue.yml
├── nginx/
│   └── nginx.conf
├── certificados/
│   ├── signing.pfx
│   └── encryption.pfx
└── Tarea4-Red Social grupo777/
```

Crea el archivo copiando la plantilla:

usualmente se debe hacer un cp pero en este caso le envie las claves y el formato que debe seguir a su whattsapp en el numero 67675067 (mucho sixseven) 
```

Abre `.env` y configura tus propios valores. Por ejemplo:

```env
# Puertos
BACKEND_PORT=5290
FRONTEND_PORT=5500

# PostgreSQL
POSTGRES_DB=RedSocialDB
POSTGRES_USER=postgres
POSTGRES_PASSWORD=tu_clave

# OpenIddict
CertificatePassword=tu_contraseña_certificados

# Autenticación externa
Authentication__Google__ClientId=tu_google_client_id
Authentication__Google__ClientSecret=tu_google_client_secret

Authentication__GitHub__ClientId=tu_github_client_id
Authentication__GitHub__ClientSecret=tu_github_client_secret
```

Si el proyecto utiliza otras integraciones, como Dropbox o Figma, agrega también las variables correspondientes que estén definidas en `.env.example`.

**Importante:**

- No compartas tu archivo `.env` públicamente.
- No reemplaces las credenciales de ejemplo por credenciales reales dentro de `.env.example`.
- Cada integrante debe crear su propio `.env`.
- Las contraseñas y variables deben coincidir con las utilizadas por la configuración de los contenedores.
- La carpeta `certificados/` debe contener los certificados PFX válidos que utiliza OpenIddict. La contraseña configurada debe corresponder a esos archivos.

Los archivos `.env` y sus credenciales no deben publicarse en GitHub.

## 3. Ejecutar utilizando las imágenes de Docker Hub

Esta es la forma recomendada para ejecutar el proyecto en otro equipo.

Las imágenes publicadas son:

| Componente | Imagen |
|---|---|
| Backend | `juliethhflores/redsocial` |
| Frontend | `juliethhflores/redsocial-frontend` |

Reemplaza `TU_USUARIO_DOCKERHUB` por el usuario propietario de las imágenes y utiliza las etiquetas que realmente hayas publicado.

### 3.1. Descargar las imágenes

Desde la raíz del proyecto, ejecuta:

```bash
como indica en el .env que le mande se debe cambiar en las variables de entorno el nombre de las imagenes ya que de forma local se llaman redsocial y redsocial-frontend pero en
docker hub juliethhflores/redsocial y juliethhflores/redsocial-frontend
Buscar esta parte en el .env que le comparti
# ----- Imágenes (solo despliegue.yml) ------------------------------------------
# Local: redsocial / redsocial-frontend con PULL_POLICY=never
# Docker Hub: TU_USUARIO/redsocial:1.0 con PULL_POLICY=always
BACKEND_IMAGE=redsocial <---Cambiar 
FRONTEND_IMAGE=redsocial-frontend   <----Cambiar 
PULL_POLICY=never
en caso de no cambiar esto dara errores debido a que no encontraria las imagenes en docker
docker compose --env-file .env -f despliegue.yml pull
```

Este comando descarga las imágenes definidas en `despliegue.yml` desde Docker Hub.

### 3.2. Iniciar los servicios

Ejecuta:

```bash
docker compose --env-file .env -f despliegue.yml up -d
```

Docker Compose iniciará los servicios definidos en el archivo:

- **Backend:** API desarrollada con ASP.NET Core.
- **Frontend:** interfaz web de la aplicación.
- **PostgreSQL:** base de datos principal.
- **Redis:** servicio de almacenamiento en memoria.
- **Migraciones:** servicio encargado de aplicar las migraciones de Entity Framework Core.
- **Nginx:** balanceador de carga que distribuye las solicitudes entre las instancias del backend.

El servicio backend depende de que las migraciones finalicen correctamente antes de iniciar.

### 3.3. Comprobar el estado

```bash
docker compose --env-file .env -f despliegue.yml ps
```

Para consultar los registros:

```bash
docker compose --env-file .env -f despliegue.yml logs -f
```

Para consultar específicamente los errores de Nginx:

```bash
docker compose --env-file .env -f despliegue.yml logs balanceador
```

### 3.4. Acceder a la aplicación

Utiliza los puertos definidos en tu archivo `.env`.

- Frontend: `http://localhost:5500`
- Backend: utiliza el puerto publicado por el balanceador, según la configuración de `BACKEND_PORT`.

## 4. Construir las imágenes con `contructor.yml`

Este archivo permite construir las imágenes a partir del código fuente y los Dockerfile del proyecto.

Se utiliza principalmente cuando se realizan cambios en el backend o frontend y se necesita generar nuevamente las imágenes.

Desde la raíz del proyecto, ejecuta:

```bash
docker compose --env-file .env -f contructor.yml build
```

Para construir y levantar los servicios directamente:

```bash
docker compose --env-file .env -f contructor.yml up -d --build
```

Este archivo utiliza las imágenes locales generadas durante la construcción.

### 4.1. Ejecutar varias instancias del backend

El proyecto permite levantar varias instancias del servicio backend para distribuir la carga mediante Nginx.

Por ejemplo, para iniciar cuatro instancias:

```bash
docker compose --env-file .env -f contructor.yml up -d --build --scale backend=4
```

Para iniciar tres instancias:

```bash
docker compose --env-file .env -f contructor.yml up -d --build --scale backend=3
```

Nginx actúa como balanceador y distribuye las solicitudes entre las instancias disponibles.

**Importante:** el servicio backend debe estar configurado para permitir escalado. Para ello, no debe tener un `container_name` fijo ni publicar directamente el mismo puerto del host en cada réplica. Debe utilizar `expose` para comunicar su puerto interno a Nginx.

Para comprobar las instancias:

```bash
docker compose --env-file .env -f contructor.yml ps
```

También puedes cambiar el número de instancias sin reconstruir las imágenes:

```bash
docker compose --env-file .env -f contructor.yml up -d --scale backend=2
```

## 5. Publicar las imágenes en Docker Hub

Cuando se realicen modificaciones y se construyan nuevas imágenes, estas pueden publicarse en Docker Hub para que otros integrantes puedan utilizarlas.

Primero, inicia sesión:

```bash
docker login
```

Etiqueta las imágenes locales:

```bash
docker tag redsocial TU_USUARIO_DOCKERHUB/redsocial:latest
docker tag redsocial-frontend TU_USUARIO_DOCKERHUB/redsocial-frontend:latest
```

Publica ambas imágenes:

```bash
docker push TU_USUARIO_DOCKERHUB/redsocial:latest
docker push TU_USUARIO_DOCKERHUB/redsocial-frontend:latest
```

Después de publicar las imágenes, los demás integrantes podrán descargarlas ejecutando:

```bash
docker compose --env-file .env -f despliegue.yml pull
```

Y levantar la aplicación con:

```bash
docker compose --env-file .env -f despliegue.yml up -d
```

El archivo `despliegue.yml` debe utilizar las imágenes de Docker Hub mediante la propiedad `image`, sin incluir `build`.

## 6. Problemas de permisos con Nginx

En algunos equipos Linux, Nginx puede presentar errores relacionados con los permisos de lectura de los archivos montados desde el proyecto.

Si aparece un error como `Permission denied` al iniciar el contenedor del balanceador, primero revisa los registros:

```bash
docker compose --env-file .env -f despliegue.yml logs balanceador
```

Comprueba que el archivo de configuración exista y tenga permisos de lectura:

```bash
ls -l nginx/
ls -l nginx/nginx.conf
```

Si el archivo existe, pero no tiene permisos suficientes, puedes corregirlos con:

```bash
chmod 755 nginx
chmod 644 nginx/nginx.conf
```

Después, reinicia el servicio:

```bash
docker compose --env-file .env -f despliegue.yml restart balanceador
```

Si estás utilizando `contructor.yml`, sustituye el nombre del archivo en los comandos anteriores.

**Recomendaciones:**

- Comprueba que `nginx/nginx.conf` sea un archivo y no un directorio.
- Verifica que el volumen esté montado correctamente en `/etc/nginx/nginx.conf`.
- Conserva el montaje de solo lectura (`:ro`) para el archivo de configuración.
- No utilices `chmod 777` como solución general, ya que concede permisos excesivos.
- Si el error menciona otra ruta, como `/var/cache/nginx` o `/var/run`, revisa los permisos de esa ruta y la configuración del contenedor antes de modificarlos.

## 7. Detener los servicios

Para detener los contenedores y eliminar la red creada por Docker Compose:

Con `despliegue.yml`:

```bash
docker compose --env-file .env -f despliegue.yml down
```

Con `contructor.yml`:

```bash
docker compose --env-file .env -f contructor.yml down
```

**Importante:** no utilices `down -v` si deseas conservar los datos almacenados en PostgreSQL.

Los datos se conservan en el volumen nombrado `datos-postgres`, siempre que no se elimine dicho volumen.

## 8. Archivos importantes

| Archivo o directorio | Descripción |
|---|---|
| `.env` | Variables de entorno y credenciales locales. |
| `.env.example` | Plantilla para crear el archivo `.env`. |
| `contructor.yml` | Construcción de imágenes y ejecución de servicios desde el código fuente. |
| `despliegue.yml` | Ejecución de las imágenes publicadas en Docker Hub. |
| `nginx/nginx.conf` | Configuración del balanceador de carga. |
| `certificados/` | Certificados utilizados por OpenIddict. |
| `Dockerfile` | Instrucciones de construcción de las imágenes. |

## 9. Verificación del funcionamiento

Para comprobar que la aplicación funciona correctamente:

1. Crear el archivo `.env` en la raíz del proyecto.
2. Configurar las credenciales y los puertos.
3. Verificar que la carpeta `certificados/` contenga los certificados válidos.
4. Descargar las imágenes desde Docker Hub utilizando `despliegue.yml`.
5. Iniciar los servicios con Docker Compose.
6. Comprobar que PostgreSQL y Redis estén funcionando.
7. Verificar que el servicio de migraciones finalice correctamente.
8. Comprobar que las instancias del backend estén activas.
9. Verificar que Nginx esté distribuyendo las solicitudes.
10. Acceder al frontend y probar las funcionalidades de la aplicación.

---

**Nota:** Para ejecutar el proyecto en otro equipo no es necesario reconstruir las imágenes. Basta con disponer de los archivos Compose, `.env`, la configuración de Nginx y los certificados necesarios, y descargar las imágenes publicadas en Docker Hub.
