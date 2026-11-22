# Guía de contribución

¡Bienvenido al proyecto Red Social!

Este documento establece las reglas y el flujo de trabajo que todos los integrantes deben seguir para contribuir al desarrollo del proyecto. El objetivo es mantener un repositorio organizado, evitar conflictos y asegurar que todas las modificaciones sean revisadas antes de incorporarse a la rama principal.

## 1. Requisitos para contribuir

Antes de comenzar, cada integrante debe:

- Tener una cuenta de GitHub.
- Tener Git instalado en su computadora.
- Realizar un Fork del repositorio original.
- Clonar su Fork en su computadora.
- Trabajar en ramas independientes.
- Enviar sus cambios mediante Pull Requests al repositorio original.

**Importante:** Los integrantes no deben ser agregados como colaboradores del repositorio original. Todas las contribuciones deben realizarse desde sus propios Forks.

## 2. Crear un Fork del repositorio

1. Ingresar al repositorio original en GitHub.
2. Presionar el botón **Fork**.
3. Seleccionar su cuenta personal como destino.
4. Esperar a que GitHub cree la copia del repositorio.

Después, clonar el Fork en la computadora:

```bash
git clone https://github.com/USUARIO/Red-Social.git
cd Red-Social
```

Reemplazar `USUARIO` por el nombre de usuario de GitHub correspondiente.

## 3. Configurar el repositorio original como upstream

Para mantener actualizado el Fork con los cambios del repositorio original, agregar el repositorio del líder como remoto:

```bash
git remote add upstream https://github.com/LIDER/Red-Social.git
```

Verificar los repositorios remotos:

```bash
git remote -v
```

Se deben visualizar dos remotos:

- `origin`: Fork personal del integrante.
- `upstream`: repositorio original del líder.

## 4. Actualizar el Fork

Antes de comenzar una nueva contribución, actualizar la rama principal local:

```bash
git checkout main
git fetch upstream
git merge upstream/main
git push origin main
```

Esto permite trabajar sobre una versión actualizada del proyecto.

## 5. Crear una rama de trabajo

Cada integrante debe crear una rama independiente para desarrollar su contribución.

Formato recomendado:

```text
feature/nombre-funcionalidad
```

Ejemplos:

- `feature/usuarios`
- `feature/publicaciones`
- `feature/comentarios`
- `feature/reacciones`

Para crear una rama:

```bash
git checkout -b feature/nombre-funcionalidad
```

No se deben realizar cambios directamente sobre `main`.

## 6. Convención para los commits

Los mensajes de los commits deben ser claros y describir brevemente los cambios realizados.

Se utilizará la siguiente estructura:

```text
tipo: descripción del cambio
```

Tipos permitidos:

| Tipo | Descripción |
|---|---|
| `feat` | Nueva funcionalidad o implementación. |
| `fix` | Corrección de errores. |
| `docs` | Cambios en la documentación. |
| `refactor` | Reorganización o mejora del código. |
| `test` | Incorporación o modificación de pruebas. |
| `style` | Cambios de formato o estilo. |

Ejemplos:

```bash
git add .
git commit -m "feat: implementar modelo de usuarios"
git commit -m "fix: corregir relacion entre publicaciones y usuarios"
git commit -m "docs: actualizar documentacion del proyecto"
```

## 7. Desarrollo de las contribuciones

Cada integrante debe implementar las tablas que le fueron asignadas, incluyendo:

- Modelo o entidad correspondiente.
- Propiedades y tipos de datos.
- Relaciones y claves foráneas necesarias.
- Incorporación de las entidades al contexto de Entity Framework Core.
- Migraciones o scripts SQL necesarios para crear las tablas.

Además, debe comprobar que sus modificaciones funcionan correctamente y no afectan las funcionalidades existentes.

Cada integrante debe implementar al menos cuatro tablas diferentes, según la distribución documentada en el README.md.

## 8. Probar los cambios

Antes de enviar una contribución, cada integrante debe:

1. Compilar el proyecto.
2. Verificar que no existan errores.
3. Comprobar las relaciones entre las tablas.
4. Verificar que las migraciones o scripts SQL funcionen.
5. Comprobar que sus cambios no interfieran con el resto del proyecto.

No se deben enviar contribuciones que contengan errores de compilación o modificaciones incompletas.

## 9. Enviar los cambios al Fork

Una vez finalizado el desarrollo y las pruebas, subir la rama al Fork personal:

```bash
git add .
git commit -m "feat: implementar tablas asignadas"
git push origin feature/nombre-funcionalidad
```

## 10. Crear un Pull Request

Para enviar la contribución al repositorio original:

1. Ingresar al Fork personal en GitHub.
2. Seleccionar la opción **Contribute** o **Compare & pull request**.
3. Configurar el Pull Request de la siguiente manera:
   - **Base repository:** repositorio original del líder.
   - **Base branch:** `develop`.
   - **Head repository:** Fork personal del integrante.
   - **Compare branch:** rama de trabajo creada por el integrante.
4. Escribir un título descriptivo.
5. Explicar los cambios realizados.
6. Crear el Pull Request.

Ejemplo de título:

```text
feat: implementar tablas de publicaciones y comentarios
```

En la descripción se debe indicar:

- Nombre del integrante.
- Tablas implementadas.
- Modelos y relaciones creadas.
- Migraciones o scripts SQL incorporados.
- Pruebas realizadas.

## 11. Revisión y aprobación de Pull Requests

Todas las contribuciones serán revisadas por el líder del repositorio antes de ser incorporadas a `main`.

El proceso será el siguiente:

1. El líder revisará el código y la documentación.
2. Si encuentra errores, solicitará los cambios necesarios.
3. El integrante realizará las correcciones en su misma rama y actualizará el Pull Request.
4. Cuando la contribución cumpla los requisitos, será aprobada y fusionada mediante **Merge Pull Request**.

En el caso de las contribuciones realizadas por el líder, otro integrante deberá realizar la revisión correspondiente. El líder será responsable de fusionar su propio Pull Request.

**Ningún integrante debe incorporar directamente sus cambios a la rama `main`.**

## 12. Reglas generales

- No modificar el trabajo de otro integrante sin coordinación previa.
- No subir archivos innecesarios, temporales o carpetas generadas como `bin` y `obj`.
- No subir contraseñas, cadenas de conexión privadas ni claves secretas.
- Mantener una estructura ordenada en el proyecto.
- Utilizar nombres descriptivos para clases, propiedades, ramas y commits.
- Comunicar los conflictos o problemas antes de realizar modificaciones que afecten a otros integrantes.
- Todas las contribuciones deben pasar por Pull Requests.

---

**Gracias por contribuir al proyecto Red Social.**
