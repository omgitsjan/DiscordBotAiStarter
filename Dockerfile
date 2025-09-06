# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY DiscordBot/*.csproj ./DiscordBot/
COPY DiscordBotTests/*.csproj ./DiscordBotTests/
RUN dotnet restore

# Copy everything else and build
COPY DiscordBot/. ./DiscordBot/
COPY DiscordBotTests/. ./DiscordBotTests/

# Run tests
WORKDIR /src/DiscordBotTests
RUN dotnet test --no-restore --verbosity normal

# Build the project
WORKDIR /src/DiscordBot
RUN dotnet publish -c Release -o /app

# Stage 2: Setup runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
