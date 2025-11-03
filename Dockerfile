# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything under src folder
COPY ./src ./src

# Restore dependencies
RUN dotnet restore "./src/CustomerService.API/CustomerService.API.csproj"

# Build and publish
WORKDIR /src/src/CustomerService.API
RUN dotnet publish "CustomerService.API.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CustomerService.API.dll"]
