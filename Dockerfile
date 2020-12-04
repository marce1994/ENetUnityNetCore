# Dockerfile

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY UDP.Server/*.csproj UDP.Server/
COPY UDP.Core/*.csproj UDP.Core/
RUN dotnet restore /UDP.Server
RUN dotnet restore /UDP.Core

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o out /UDP.Server

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .

# Run the app on container startup
# Use your project name for the second parameter
# e.g. MyProject.dll
ENTRYPOINT [ "dotnet", "UDP.Server.dll" ]