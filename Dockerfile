# ========================================================
# ETAPA 1: BASE (El entorno ligero de producción)
# ========================================================
# Usamos ASP.NET Runtime: solo sirve para ejecutar, no para compilar.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# A partir de .NET 8/9/10, las imágenes oficiales escuchan por defecto en el puerto 8080
EXPOSE 8080

# ========================================================
# ETAPA 2: BUILD (El taller pesado con el SDK)
# ========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# --- TRUCO DEL CACHÉ DE NUGET ---
# Primero copiamos SOLO los archivos de definición (.slnx / .csproj).
# Si cambias código C# mañana, Docker salta el restore porque estos archivos no cambiaron.
# 1. Copiamos todos los .csproj respetando la ruta src/
COPY ["src/FinTrack.Domain/FinTrack.Domain.csproj", "src/FinTrack.Domain/"]
COPY ["src/FinTrack.Application/FinTrack.Application.csproj", "src/FinTrack.Application/"]
COPY ["src/FinTrack.Infrastructure/FinTrack.Infrastructure.csproj", "src/FinTrack.Infrastructure/"]
COPY ["src/FinTrack.Web/FinTrack.Web.csproj", "src/FinTrack.Web/"]
# (Si tienes más proyectos como FinTrack.Core o FinTrack.Infrastructure, añade sus COPY aquí)

# 2. Restauramos apuntando a la ruta real
RUN dotnet restore "src/FinTrack.Web/FinTrack.Web.csproj"

# Ahora copiamos todo el resto del código fuente (el .dockerignore evitará basura como bin/ y obj/)
COPY . .

# 4. Compilamos apuntando al Web.csproj dentro de src/
WORKDIR "/src/src/FinTrack.Web"
RUN dotnet build "FinTrack.Web.csproj" -c Release -o /app/build

# ========================================================
# ETAPA 3: PUBLISH (Generar los binarios limpios)
# ========================================================
FROM build AS publish
RUN dotnet publish "FinTrack.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ========================================================
# ETAPA 4: FINAL (La imagen final que correrá en producción)
# ========================================================
# Regresamos a la imagen limpia y ligera del inicio (base)
FROM base AS final
WORKDIR /app

# Copiamos ÚNICAMENTE los archivos publicados desde la etapa 'publish'
COPY --from=publish /app/publish .

# Comando que arranca tu aplicación al encender el contenedor
ENTRYPOINT ["dotnet", "FinTrack.Web.dll"]