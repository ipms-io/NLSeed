FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NLSeed.csproj", "NLSeed.csproj"]
RUN dotnet restore "NLSeed.csproj" --no-cache

COPY . .

RUN dotnet build "NLSeed.csproj" -c Release --no-restore
RUN dotnet publish "NLSeed.csproj" -c Release -o /app/publish --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN sed -i 's/^app:x:1654:1654:/app:x:1000:1000:/' /etc/passwd && \
    sed -i 's/^app:x:1654:/app:x:1000:/' /etc/group
RUN export APP_UID=1000

WORKDIR /home/app

COPY --from=build --chown=app:app /app/publish .

USER 1000

ENTRYPOINT ["dotnet", "NLSeed.dll"]