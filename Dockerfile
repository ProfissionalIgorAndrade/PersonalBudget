# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PersonalBudget.sln", ""]
COPY ["PersonalBudget.Api/PersonalBudget.Api.csproj", "PersonalBudget.Api/"]
COPY ["PersonalBudget.Application/PersonalBudget.Application.csproj", "PersonalBudget.Application/"]
COPY ["PersonalBudget.Domain/PersonalBudget.Domain.csproj", "PersonalBudget.Domain/"]

# Restore dependencies
RUN dotnet restore "PersonalBudget.sln"

# Copy source code
COPY . .

# Build the project
WORKDIR "/src/PersonalBudget.Api"
RUN dotnet build "PersonalBudget.Api.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "PersonalBudget.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose port (match your launchSettings.json)
EXPOSE 5047

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5047
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "PersonalBudget.Api.dll"]
