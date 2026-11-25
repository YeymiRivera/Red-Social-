FROM nginx:1.28-alpine

# Copiar la portada de Red Social encima del index.html predeterminado de Nginx
COPY index.html /usr/share/nginx/html/index.html

# Copiar página de Login
COPY login/ /usr/share/nginx/html/login/

# Página de Bienvenida y sus recursos
COPY Bienvenida/ /usr/share/nginx/html/Bienvenida/

# Script que escribe js/config.js desde la variable API_URL (nginx ejecuta
# /docker-entrypoint.d/*.sh antes de arrancar). El sed quita finales de línea CRLF.
COPY docker-entrypoint.d/40-config-js.sh /docker-entrypoint.d/40-config-js.sh
RUN sed -i 's/\r$//' /docker-entrypoint.d/40-config-js.sh \
    && chmod +x /docker-entrypoint.d/40-config-js.sh

ENV API_URL=http://localhost:5290

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
