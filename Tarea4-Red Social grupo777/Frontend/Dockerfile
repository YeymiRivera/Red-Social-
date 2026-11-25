FROM nginx:alpine

# Copiar la portada de Red Social encima del index.html predeterminado de Nginx
COPY index.html /usr/share/nginx/html/index.html

# Copiar página de Login
COPY login/ /usr/share/nginx/html/login/

# Copiar página de Bienvenida
COPY Bienvenida/ /usr/share/nginx/html/Bienvenida/

# Puerto interno de Nginx
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]