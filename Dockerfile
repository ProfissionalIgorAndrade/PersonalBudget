# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["PersonalBudget.sln", ""]
COPY ["PersonalBudget.Api/PersonalBudget.Api.csproj", "PersonalBudget.Api/"]
COPY ["PersonalBudget.Application/PersonalBudget.Application.csproj", "PersonalBudget.Application/"]
COPY ["PersonalBudget.Domain/PersonalBudget.Domain.csproj", "PersonalBudget.Domain/"]
COPY ["PersonalBudget.Infrastructure/PersonalBudget.Infrastructure.csproj", "PersonalBudget.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "PersonalBudget.Api/PersonalBudget.Api.csproj"

# Copy source code
COPY . .

# Publish
WORKDIR "/src/PersonalBudget.Api"
RUN dotnet publish "PersonalBudget.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Expose Fly default port
EXPOSE 8080

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production

# Run
ENTRYPOINT ["dotnet", "PersonalBudget.Api.dll"]
