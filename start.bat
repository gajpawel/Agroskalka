@echo off
chcp 65001 >nul
title Agroskalka - Srodowisko Deweloperskie

echo ========================================================
echo  URUCHAMIANIE PROJEKTU AGROSKALKA
echo ========================================================

echo [1/3] Uruchamianie kontenerow Docker (PostgreSQL, Keycloak, Mailpit, Stripe CLI)...
docker compose up -d
if %errorlevel% neq 0 (
    echo [BLAD] Nie udalo sie uruchomic kontenerow. Upewnij sie, ze Docker Desktop jest wlaczony!
    pause
    exit /b %errorlevel%
)

echo [2/3] Uruchamianie kompilatora Tailwind CSS w tle...
start "Agroskalka - Tailwind CSS" cmd /k "npx @tailwindcss/cli -i ./wwwroot/css/app.css -o ./wwwroot/css/site.css --watch"

echo [3/3] Uruchamianie serwera ASP.NET Core (Hot Reload)...
dotnet watch --launch-profile https