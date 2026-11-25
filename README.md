# Agroskałka - System sprzedaży biletów wstępu

Aplikacja webowa wspierająca proces rezerwacji oraz zakupu biletów do wiejskiego parku rozrywki **Agroskałka**. Projekt oparty jest o platformę **ASP.NET Core 8** (Razor Pages / Web API) oraz bazę danych **PostgreSQL** z wykorzystaniem **Entity Framework Core**. Interfejs użytkownika korzysta z **Tailwind CSS**, uwierzytelnianie opiera się o **Keycloak**, a płatności online obsługuje bramka **Stripe**.

---

## 🛠️ Wymagania wstępne

Przed pierwszym uruchomieniem upewnij się, że masz zainstalowane:
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (obsługuje bazę danych PostgreSQL, Keycloak, Mailpit oraz tunel webhooków Stripe CLI)
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js](https://nodejs.org/) (wersja LTS — wymagany do kompilacji Tailwind CSS)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) / Rider / VS Code (opcjonalnie do edycji kodu)

*(Nie musisz ręcznie instalować PostgreSQL ani Stripe CLI na swoim komputerze — działają w kontenerach Docker).*

---

## ⚙️ Pierwsza konfiguracja po sklonowaniu repozytorium

### 1. Przywrócenie zależności (NuGet & npm)
W katalogu głównym projektu wykonaj:
```shell
dotnet restore
npm install

```

---

### 2. Konfiguracja pliku `.env`

> ⚠️ **Uwaga:** Plik `.env` jest ignorowany przez Git i nie trafia do repozytorium.

W katalogu głównym projektu (obok pliku `Agroskalka.csproj`) utwórz plik `.env` i uzupełnij go według poniższego wzorca:

```env
# Baza danych PostgreSQL (uruchamiana w Dockerze)
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

#### Jak uzupełnić klucze w `.env`:

1. **CAPTCHA:** Utwórz widget Cloudflare Turnstile dla domeny aplikacji i wklej `SITEKEY` oraz `SECRETKEY`.
2. **Stripe:**
* Zaloguj się do [Stripe Dashboard](https://dashboard.stripe.com/test/apikeys), przełącz panel w **Test mode** i skopiuj `Secret key` (`sk_test_...`) oraz `Publishable key` (`pk_test_...`).
* W [Settings $\to$ Payment methods](https://dashboard.stripe.com/test/settings/payment_methods) włącz **Cards**, **BLIK** oraz **Przelewy24**.
* Po pierwszym uruchomieniu projektu (krok niżej) sprawdź logi kontenera Stripe poleceniem `docker logs agroskalka-stripe`, skopiuj wyświetlony klucz `whsec_...`, wklej go jako `STRIPE_WEBHOOK_SECRET` w `.env` i zrestartuj aplikację.


3. **Keycloak (`OIDC_CLIENT_SECRET`):** Wygeneruj losowy 32-bajtowy klucz w terminalu i wklej do `.env`:
* **PowerShell:**
```powershell
[byte[]]$b = [byte[]]::new(32); [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); ([BitConverter]::ToString($b) -replace '-','').ToLower()

```


* **Bash / Linux / macOS:**
```shell
openssl rand -hex 32

```





---

## 🚀 Uruchomienie projektu

1. Upewnij się, że **Docker Desktop** jest włączony.
2. Kliknij dwukrotnie plik **`start.bat`** w głównym folderze projektu (lub wpisz `.\start.bat` w terminalu).

Skrypt automatycznie:

1. Postawi kontenery **PostgreSQL**, **Keycloak**, **Mailpit** oraz **Stripe CLI**.
2. Uruchomi w osobnym oknie kompilator **Tailwind CSS** w trybie `--watch`.
3. Uruchomi aplikację **ASP.NET Core (`dotnet watch`)** oraz automatycznie nałoży migracje bazy danych wraz z danymi początkowymi.

*(Jeśli pracujesz z poziomu Visual Studio: uruchom raz `docker compose up -d` w terminalu, a następnie uruchamiaj aplikację standardowo klawiszem `F5`).*

---

## 🌐 Przydatne adresy lokalne i dane testowe

* **Aplikacja Agroskałka:** `https://localhost:7251` (lub port wskazany w konsoli przy starcie)
* **Panel administracyjny Keycloak:** [http://localhost:8080](http://localhost:8080) (login/hasło z `KC_BOOTSTRAP_ADMIN_*` w `.env`)
* **Lokalna skrzynka pocztowa (Mailpit):** [http://localhost:8025](http://localhost:8025) (przechwytuje wszystkie maile z rejestracji)
* **Testowa karta płatnicza Stripe:** numer `4242 4242 4242 4242`, dowolna przyszła data ważności i kod CVC.

### Domyślne konta (Seeder / Keycloak)

| Rola | Email | Hasło |
| --- | --- | --- |
| **Administrator (lokalne)** | `account@example.com` | `Haslo1` |
| **Administrator (Keycloak)** | `admin@agroskalka.pl` | `admin` |

### Resetowanie kontenerów

Jeśli zmienisz hasła lub `OIDC_CLIENT_SECRET` w pliku `.env` już po pierwszym uruchomieniu, zresetuj wolumeny Dockera komendą:

```shell
docker compose down -v

```

a następnie ponownie uruchom `start.bat`.