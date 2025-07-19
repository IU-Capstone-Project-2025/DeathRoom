# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy the solution file and project files
COPY *.sln ./
COPY global.json ./
COPY DeathRoom-Backend/*.csproj ./DeathRoom-Backend/
COPY DeathRoom.Common/*.csproj ./DeathRoom.Common/
COPY DeathRoom.Application/*.csproj ./DeathRoom.Application/
COPY DeathRoom.Domain/*.csproj ./DeathRoom.Domain/
COPY DeathRoom.Infrastructure/*.csproj ./DeathRoom.Infrastructure/
COPY DeathRoom.Tests/*.csproj ./DeathRoom.Tests/

# Restore dependencies
RUN dotnet restore

# Copy the source code
COPY . .

# Build the application
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish DeathRoom-Backend/DeathRoom-Backend.csproj -c Release -o /app/publish

# Use the runtime image for the final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0

# Set the working directory
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Expose the port (if your server uses a specific port)
EXPOSE 9050

# Set the entry point
ENTRYPOINT ["dotnet", "DeathRoom-Backend.dll"]
