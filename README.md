# Agroskałka - System sprzedaży biletów wstępu

Aplikacja webowa wspierająca proces rezerwacji oraz zakupu biletów do wiejskiego parku rozrywki **Agroskałka**. Projekt oparty jest o platformę **ASP.NET Core 8** (Razor Pages / Web API) oraz bazę danych **PostgreSQL** z wykorzystaniem **Entity Framework Core**. Interfejs użytkownika korzysta z **Tailwind CSS**, a płatności online obsługiwane są przez bramkę **Stripe**.

---

## 🛠️ Wymagania wstępne

Przed uruchomieniem upewnij się, że na swojej stacji roboczej masz zainstalowane:
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js](https://nodejs.org/) (wersja LTS) oraz menedżer pakietów **npm** (niezbędne do kompilacji Tailwind CSS)
* [PostgreSQL](https://www.postgresql.org/download/) (wersja 14 lub nowsza) lub uruchomiony kontener Docker z PostgreSQL
* pgAdmin4
* [Stripe CLI](https://docs.stripe.com/stripe-cli) (wymagane do lokalnego testowania webhooków płatności)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (wymagany do uruchomienia lokalnego serwera uwierzytelniania Keycloak)
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

### 2. Konfiguracja zmiennych środowiskowych (`.env`)

> ⚠️ **Uwaga:** Pliki z poświadczeniami (`.env`) są ignorowane przez Git i nie trafiają do repozytorium.

Projekt obsługuje zmienne środowiskowe z pliku `.env`. W katalogu głównym projektu (obok pliku `Agroskalka.csproj`) utwórz plik `.env` na podstawie poniższego wzorca:

```env
# Baza danych PostgreSQL
DB_HOST=localhost
DB_PORT=5432
DB_NAME=agroskalka
DB_USER=postgres
DB_PASSWORD=twoje_haslo_do_bazy

# Cloudflare Turnstile (CAPTCHA)
CAPTCHA__ENABLED=true
CAPTCHA__SITEKEY=klucz_publiczny_Cloudflare_Turnstile
CAPTCHA__SECRETKEY=klucz_prywatny_Cloudflare_Turnstile

# Bramka płatności Stripe (Tryb testowy)
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...

# Integracja OIDC (Keycloak)
OIDC_AUTHORITY=http://localhost:8080/realms/agroskalka
OIDC_CLIENT_ID=agroskalka-web
OIDC_CLIENT_SECRET=wygenerowany_sekret

# Dane logowania do panelu administracyjnego Keycloak
KC_BOOTSTRAP_ADMIN_USERNAME=admin
KC_BOOTSTRAP_ADMIN_PASSWORD=admin

# Dane do logowania na konto testowe administratora aplikacji przez Keycloak
KC_TEST_ADMIN_USERNAME=admin@agroskalka.pl
KC_TEST_ADMIN_PASSWORD=admin
```

* **CAPTCHA:** Domyślnie włączona dla rejestracji oraz logowania hasłem. Utwórz widget Cloudflare Turnstile dla domeny aplikacji i ustaw klucze w `.env`.
* **Stripe:** Zaloguj się do [Stripe Dashboard](https://dashboard.stripe.com/test/apikeys), przełącz panel w **Test mode** i skopiuj `Secret key` (`sk_test_...`) oraz `Publishable key` (`pk_test_...`).
* **Keycloak (`OIDC_CLIENT_SECRET`):** Wygeneruj bezpieczny, kryptograficznie losowy sekret i wklej go jako wartość `OIDC_CLIENT_SECRET` w pliku `.env`. Możesz go wygenerować jedną z poniższych komend (wyświetli gotowy klucz w terminalu do skopiowania):
  * **PowerShell (Windows):**
    ```shell
    [byte[]]$b = [byte[]]::new(32); [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); ([BitConverter]::ToString($b) -replace '-','').ToLower()
    ```
  * **Git Bash / Linux / macOS:**
    ```shell
    openssl rand -hex 32
    ```

---

### 3. Konfiguracja bramki płatności Stripe (Test Mode)

1. **Włączenie metod płatności:**
   W [Stripe Dashboard](https://dashboard.stripe.com/test/settings/payment_methods) przejdź do **Settings $\to$ Payment methods** i upewnij się, że włączone są:
   * **Cards** (Karty płatnicze)
   * **BLIK**
   * **Przelewy24**

2. **Konfiguracja lokalnego Webhooka (Stripe CLI):**
   Aby aplikacja automatycznie zmieniała status biletu na opłacony (`IsPaid = true`) po zakończeniu płatności na `localhost`, musisz przekierować zdarzenia ze Stripe za pomocą Stripe CLI:
   * Zaloguj się w terminalu:
     ```shell
     stripe login
     ```
   * Uruchom nasłuchiwanie i przekierowanie na adres lokalnego kontrolera webhooków (dopasuj numer portu z pliku `Properties/launchSettings.json`, np. `7251`):
     ```shell
     stripe listen --forward-to https://localhost:7251/api/webhooks/stripe --skip-verify --all-snapshot
     ```
   * Skopiuj wygenerowany przez komendę klucz podpisu (zaczynający się od `whsec_...`) i wklej go do pliku `.env` jako wartość zmiennej `STRIPE_WEBHOOK_SECRET`, a następnie zrestartuj aplikację.
   * Aby przetestować płatność, użyj deweloperskiej karty płatniczej o numerze 4242 4242 4242 4242, data ważności dowolna większa niż dzisiaj, pozostałe dane dowolne spełniające wymagania formularza

---

### 4. Utworzenie bazy danych i aplikowanie migracji

1. Upewnij się, że usługa PostgreSQL jest aktywna.
2. Utwórz pustą bazę danych (np. przez pgAdmin, psql lub terminal):
```shell
# W psql:
CREATE DATABASE agroskalka;
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

### 5. Uruchomienie Keycloak (Docker)

Upewnij się, że masz uruchomiony **Docker Desktop** oraz uzupełniony plik `.env` (w szczególności `OIDC_CLIENT_SECRET`). W katalogu głównym projektu uruchom kontener:

```shell
docker compose up -d
```

Przy pierwszym uruchomieniu Keycloak automatycznie zaimportuje konfigurację realmu `agroskalka` z pliku `agroskalka-realm.json` i podstawi wartości z `.env`.
* **Panel administracyjny Keycloak:** `http://localhost:8080` (dane logowania z `KC_BOOTSTRAP_ADMIN_*` w `.env`).
* **Reset po zmianie sekretu lub danych w `.env`:** Import pliku JSON wykonuje się tylko na pustym wolumenie. Jeśli zmienisz `OIDC_CLIENT_SECRET` lub dane testowe już po pierwszym uruchomieniu, postaw kontener na czysto:
  ```shell
  docker compose down -v
  docker compose up -d
  ```

---

## 🚀 Uruchomienie projektu (Tryb Deweloperski)

> Przed startem upewnij się, że kontener Keycloak działa w tle (`docker compose up -d`).

Aby widzieć zmiany w kodzie C# (Hot Reload), style Tailwind CSS oraz odbierać potwierdzenia płatności ze Stripe w czasie rzeczywistym, uruchom trzy procesy w osobnych oknach terminala:

**Terminal 1 (Kompilacja Tailwind CSS w tle):**
```shell
npx @tailwindcss/cli -i ./wwwroot/css/app.css -o ./wwwroot/css/site.css --watch
```

**Terminal 2 (Tunel webhooków Stripe):**
```shell
stripe listen --forward-to https://localhost:7251/api/webhooks/stripe --skip-verify --all-snapshot
```

**Terminal 3 (Uruchomienie serwera .NET z Hot Reload):**
```shell
dotnet watch
```

Jeśli nie potrzebujesz odświeżania na żywo, aplikację można uruchomić standardowo klawiszem `F5` / `Ctrl + F5` w Visual Studio (pamiętając o jednorazowym zbudowaniu stylów komendą `npx @tailwindcss/cli -i ./wwwroot/css/app.css -o ./wwwroot/css/site.css`).

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

Wszystkie tabele oraz kolumny w bazie PostgreSQL generowane są automatycznie w formacie **`snake_case`** za pośredwem pakietu `EFCore.NamingConventions`. W kodzie C# zachowujemy standardowy **`PascalCase`**.