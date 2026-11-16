# ETAPA 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copiar el proyecto y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore
# Copiar el resto de archivos
COPY . .
# Compilar en Release
RUN dotnet build -c Release -o /app/build --no-restore

# ETAPA 2: Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
# Copiar archivos compilados
COPY --from=build /app/build .
# Puerto de escucha
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Iniciar la API
ENTRYPOINT ["dotnet", "Tarea4.dll"]