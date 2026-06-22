# Stage 1: Build Angular frontend
FROM node:20-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm ci
COPY frontend/ ./
RUN npm run build -- --configuration production

# Stage 2: Build and publish ASP.NET Core API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src
COPY backend/ ./backend/
RUN dotnet restore backend/ABR.Api/ABR.Api.csproj
RUN dotnet publish backend/ABR.Api/ABR.Api.csproj -c Release -o /app/publish --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=api-build /app/publish .
COPY --from=frontend-build /src/frontend/dist/abr-frontend/browser ./wwwroot

EXPOSE 8080
ENTRYPOINT ["dotnet", "ABR.Api.dll"]
