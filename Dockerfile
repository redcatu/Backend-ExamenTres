# Fase base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Configuramos el puerto 8080 que usará Railway
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
WORKDIR /app

# Fase de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiamos el csproj y restauramos (usando la ruta plana: "./")
COPY ["ExamenTresBE.csproj", "./"]
RUN dotnet restore "./ExamenTresBE.csproj"

# Copiamos todo el código
COPY . .

# Build
RUN dotnet build "./ExamenTresBE.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publicación
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ExamenTresBE.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Fase final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExamenTresBE.dll"]