# Agroskałka - System sprzedaży biletów wstępu

Aplikacja webowa wspierająca proces rezerwacji oraz zakupu biletów do wiejskiego parku rozrywki **Agroskałka**. Projekt oparty jest o platformę **ASP.NET Core 8** (Razor Pages / Web API) oraz bazę danych **PostgreSQL** z wykorzystaniem **Entity Framework Core**. Interfejs użytkownika korzysta z **Tailwind CSS**.

---

## 🛠️ Wymagania wstępne

Przed uruchomieniem upewnij się, że na swojej stacji roboczej masz zainstalowane:
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js](https://nodejs.org/) (wersja LTS) oraz menedżer pakietów **npm** (niezbędne do kompilacji Tailwind CSS)
* [PostgreSQL](https://www.postgresql.org/download/) (wersja 14 lub nowsza) lub uruchomiony kontener Docker z PostgreSQL
* pgAdmin4
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (z pakietem *ASP.NET and web development*) lub Visual Studio Code / Rider
* Globalne narzędzie EF Core CLI (opcjonalnie, jeśli używasz terminala):
  ```shell
  dotnet tool install --global dotnet-ef
  ```

---

## ⚙️ Pierwsza konfiguracja po sklonowaniu repozytorium

### 1. Przywrócenie zależności (NuGet & npm)

W katalogu głównym projektu wykonaj przywracanie pakietów backendowych oraz frontendowych:

```shell
dotnet restore
npm install
```

---

### 2. Konfiguracja połączenia z bazą danych

> ⚠️ **Uwaga:** Pliki z poświadczeniami (`.env`) są ignorowane przez Git i nie trafiają do repozytorium.

Projekt obsługuje zmienne środowiskowe z pliku `.env`. W katalogu głównym projektu (obok pliku `Agroskalka.csproj`) utwórz plik `.env` na podstawie poniższego wzorca:

```env
DB_HOST=localhost
DB_PORT=5432
DB_NAME=agroskalka
DB_USER=postgres
DB_PASSWORD=twoje_haslo_do_bazy
```

### 3. Utworzenie bazy danych i aplikowanie migracji

1. Upewnij się, że usługa PostgreSQL jest aktywna.
2. Utwórz pustą bazę danych (np. przez pgAdmin, psql lub terminal):
```shell
# W psql:
CREATE DATABASE agroskalka_db;
```

3. Zaaplikuj migracje EF Core oraz domyślne dane początkowe (seeder):
* **W Visual Studio (Package Manager Console):**
```powershell
Update-Database
```

* **W standardowym terminalu (.NET CLI):**
```shell
dotnet ef database update
```

---

## 🚀 Uruchomienie projektu (Tryb Deweloperski)

Aby widzieć zmiany w kodzie C# (Hot Reload) oraz zmiany w stylach Tailwind CSS w czasie rzeczywistym, zaleca się uruchomienie projektu w dwóch osobnych oknach terminala:

**Terminal 1 (Kompilacja Tailwind CSS w tle):**
```shell
npx @tailwindcss/cli -i ./wwwroot/css/app.css  -o ./wwwroot/css/site.css --watch
```

**Terminal 2 (Uruchomienie serwera .NET z Hot Reload):**
```shell
dotnet watch
```

Jeśli nie potrzebujesz odświeżania na żywo, aplikację można uruchomić standardowo:
* Klawiszem `F5` / `Ctrl + F5` w środowisku Visual Studio.
* Komendą `dotnet run` z poziomu terminala. Zbuduj wcześniej style jednorazowo komendą: `npx @tailwindcss/cli -o ./wwwroot/css/site.css`.

Domyślnie aplikacja uruchamia się pod adresem: `https://localhost:7xxx` lub `http://localhost:5xxx`.

---

## 👤 Domyślne dane dostępowe (Seeder)

Po nałożeniu migracji baza danych zostaje automatycznie zainicjalizowana domyślnymi rolami oraz kontem administratora:

| Rola | Email | Hasło | Id roli |
| --- | --- | --- | --- |
| **Administrator** | `account@example.com` | `Haslo1` | `1` |
| **Klient** | *(do rejestracji)* | — | `2` |

---

## 📂 Konwencje bazodanowe

Wszystkie tabele oraz kolumny w bazie PostgreSQL generowane są automatycznie w formacie **`snake_case`** za pośrednictwem pakietu `EFCore.NamingConventions`. W kodzie C# zachowujemy standardowy **`PascalCase`**.