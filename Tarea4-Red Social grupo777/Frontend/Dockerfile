FROM nginx:alpine

# Copiar página de Login
COPY login/ /usr/share/nginx/html/login/

# Copiar página de Bienvenida
COPY Bienvenida/ /usr/share/nginx/html/Bienvenida/

# Puerto interno de Nginx
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]