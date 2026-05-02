# GameOrganizer

Aplikacja webowa do zarządzania kolekcją gier wideo. Umożliwia użytkownikom śledzenie biblioteki gier, tworzenie własnych kolekcji, porównywanie zbiorów ze znajomymi oraz komunikację w czasie rzeczywistym.

## Spis treści

- [Funkcje](#funkcje)
- [Technologie](#technologie)
- [Architektura](#architektura)
- [Wymagania](#wymagania)
- [Uruchomienie](#uruchomienie)
  - [Docker (zalecane)](#docker-zalecane)
  - [Lokalne uruchomienie](#lokalne-uruchomienie)
- [Konfiguracja](#konfiguracja)
- [API Endpoints](#api-endpoints)
- [Autoryzacja](#autoryzacja)
- [Baza danych](#baza-danych)
- [Real-time (SignalR)](#real-time-signalr)

## Funkcje

- **Biblioteka gier** – przeglądanie i przeszukiwanie globalnej bazy gier
- **Kolekcje** – tworzenie własnych folderów/kategorii z grami, udostępnianie kolekcji za pomocą unikalnego linku
- **Status gry** – oznaczanie gier jako: Zagrane, W trakcie, Chcę zagrać itp.
- **Znajomi** – wysyłanie zaproszeń, porównywanie bibliotek, przeglądanie kolekcji znajomego
- **Czat** – wiadomości w czasie rzeczywistym (czaty grupowe) za pośrednictwem SignalR
- **Propozycje gier** – użytkownicy mogą proponować nowe tytuły; administrator zatwierdza lub odrzuca
- **Statystyki** – podgląd statystyk własnej biblioteki oraz danych globalnych
- **Panel administratora** – zarządzanie użytkownikami, przeglądanie logów historii, moderacja gier
- **Uwierzytelnianie** – JWT + OAuth2 (Google)

## Technologie

| Warstwa | Technologia |
|---------|-------------|
| Runtime | .NET 9.0 / ASP.NET Core Web API |
| Baza danych | PostgreSQL 17 |
| ORM | Entity Framework Core 9.0 |
| Uwierzytelnianie | ASP.NET Core Identity + JWT Bearer + Google OAuth2 |
| Real-time | SignalR (WebSocket) |
| Obrazy | Cloudinary |
| Monitoring | Sentry |
| Dokumentacja API | Swagger / OpenAPI |
| Konteneryzacja | Docker + Docker Compose |

## Architektura

Projekt to monolityczna aplikacja REST API zbudowana w ASP.NET Core z następującą strukturą katalogów:

```
GameOrganizer.Api/
├── Controllers/         # Kontrolery REST API + Admin Panel
├── Hubs/                # SignalR Hubs (Chat, Notification)
├── Models/              # Encje EF Core, DTO, enumy
├── Services/            # Logika biznesowa (interfejsy + implementacje)
├── Seeders/             # Seed danych (gatunki, platformy, przykładowe gry)
├── Migrations/          # Migracje EF Core
├── Sentry/              # Konfiguracja monitoringu
└── Templates/           # Szablony emaili (HTML)
```
## Wymagania

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (do uruchomienia przez Docker)
- **lub** .NET SDK 9.0 + PostgreSQL 17 (do uruchomienia lokalnego)

## Uruchomienie

### Docker (zalecane)

1. Sklonuj repozytorium:
   ```bash
   git clone <repo-url>
   cd game-organizer
   ```

2. Utwórz plik `.env` w katalogu `game-organizer/` na podstawie sekcji [Konfiguracja](#konfiguracja).

3. Uruchom kontenery:
   ```bash
   docker compose up --build
   ```

4. API będzie dostępne pod adresem:
   - HTTP: `http://localhost:8080`
   - HTTPS: `https://localhost:8081`

5. Swagger UI (tylko środowisko deweloperskie): `http://localhost:8080/swagger`

### Lokalne uruchomienie

1. Upewnij się, że PostgreSQL jest uruchomiony lokalnie.

2. Uzupełnij `appsettings.Development.json` lub zmienne środowiskowe (patrz [Konfiguracja](#konfiguracja)).

3. Zastosuj migracje:
   ```bash
   dotnet ef database update --project GameOrganizer.Api
   ```

4. Uruchom aplikację:
   ```bash
   dotnet run --project GameOrganizer.Api
   ```

## Konfiguracja

Aplikacja wczytuje konfigurację z pliku `.env` (w Dockerze) lub zmiennych środowiskowych / `appsettings.json`.

| Zmienna | Opis |
|---------|------|
| `DefaultConnection` | Connection string do PostgreSQL (Docker) |
| `DefaultConnection_LOCAL` | Connection string do PostgreSQL (lokalnie) |
| `JWT__Secret` | Klucz do podpisywania tokenów JWT |
| `JWT__Issuer` | Issuer tokenu JWT |
| `JWT__Audience` | Audience tokenu JWT |
| `GOOGLE_CLIENT_ID` | ID klienta Google OAuth2 |
| `GOOGLE_CLIENT_SECRET` | Sekret klienta Google OAuth2 |
| `CLOUDINARY_CLOUD_NAME` | Nazwa konta Cloudinary |
| `CLOUDINARY_API_KEY` | Klucz API Cloudinary |
| `CLOUDINARY_API_SECRET` | Sekret API Cloudinary |
| `Sentry__Dsn` | DSN do projektu Sentry |
| `FRONTEND_URL` | URL frontendu (dla CORS i przekierowań OAuth) |

> Zmienna `IS_IN_CONTAINER=true` jest ustawiana automatycznie przez Docker Compose i decyduje, który connection string zostanie użyty.

## API Endpoints

### Uwierzytelnianie – `/api/authentication`

| Metoda | Endpoint | Opis | Auth |
|--------|----------|------|------|
| POST | `/register` | Rejestracja nowego użytkownika | ❌ |
| POST | `/login` | Logowanie (limit: 5 prób/min) | ❌ |
| POST | `/forgot-password` | Inicjowanie resetowania hasła | ❌ |
| POST | `/reset-password` | Resetowanie hasła z tokenem | ❌ |
| GET | `/external-login` | Inicjowanie logowania OAuth (Google) | ❌ |
| GET | `/external-login-callback` | Callback OAuth | ❌ |
| GET | `/me` | Profil zalogowanego użytkownika | ✅ |
| PUT | `/update-profile` | Aktualizacja profilu (imię, avatar) | ✅ |
| POST | `/logout` | Wylogowanie | ✅ |

### Gry – `/api/games`

| Metoda | Endpoint | Opis | Auth | Rola |
|--------|----------|------|------|------|
| POST | `/create-game` | Dodaj grę do bazy | ✅ | Admin |
| PUT | `/update-game` | Zaktualizuj grę | ✅ | Admin |
| POST | `/available-table` | Paginowana lista gier | ✅ | - |
| POST | `/add-to-collection/{gameId}` | Dodaj grę do kolekcji | ✅ | - |
| POST | `/propose` | Zaproponuj nową grę | ✅ | - |
| POST | `/move-game` | Przenieś grę między kolekcjami | ✅ | - |
| GET | `/genres` | Lista gatunków | ✅ | - |
| GET | `/platforms` | Lista platform | ✅ | - |

### Kolekcje – `/api/collections`

| Metoda | Endpoint | Opis | Auth |
|--------|----------|------|------|
| GET | `/lookup` | Lista kolekcji użytkownika | ✅ |
| POST | `/grouped-with-games` | Kolekcje z grami (DataTable) | ✅ |
| POST | `/create` | Utwórz kolekcję | ✅ |
| PUT | `/update` | Aktualizuj kolekcję | ✅ |
| DELETE | `/delete/{id}` | Usuń kolekcję | ✅ |
| GET | `/share/{shareCode}` | Przeglądaj udostępnioną kolekcję | ❌ |

### Znajomi – `/api/friends`

| Metoda | Endpoint | Opis | Auth |
|--------|----------|------|------|
| POST | `/add-by-username/{username}` | Wyślij zaproszenie do znajomych | ✅ |
| GET | `/my-friends` | Lista zaakceptowanych znajomych | ✅ |
| POST | `/send-invite` | Wyślij email z zaproszeniem do rejestracji | ✅ |
| POST | `/search` | Wyszukaj użytkowników | ✅ |
| GET | `/pending-requests` | Oczekujące zaproszenia | ✅ |
| POST | `/accept/{requesterId}` | Zaakceptuj zaproszenie | ✅ |
| DELETE | `/reject-or-remove/{friendId}` | Odrzuć lub usuń znajomego | ✅ |
| GET | `/{friendId}/collections-with-games` | Publiczne kolekcje znajomego | ✅ |
| GET | `/compare/{friendId}` | Porównaj biblioteki gier | ✅ |

### Czat – `/api/chat`

| Metoda | Endpoint | Opis | Auth |
|--------|----------|------|------|
| GET | `/my-chats` | Lista czatów użytkownika | ✅ |
| GET | `/{groupId}/messages` | Historia wiadomości | ✅ |
| POST | `/create` | Utwórz nową rozmowę / grupę | ✅ |

### Statystyki – `/api/statistics`

| Metoda | Endpoint | Opis | Auth |
|--------|----------|------|------|
| GET | `/my-library` | Statystyki własnej biblioteki | ✅ |
| GET | `/global` | Statystyki globalne | ✅ |

### Panel administratora

| Metoda | Endpoint | Opis |
|--------|----------|------|
| GET | `/api/adminPanel/users/roles` | Lista ról |
| POST | `/api/adminPanel/users/get-all-users` | Lista użytkowników (DataTable) |
| POST | `/api/adminPanel/users/create-user` | Utwórz użytkownika |
| POST | `/api/adminPanel/users/update-user` | Zaktualizuj użytkownika |
| POST | `/api/adminPanel/users/lock-user/{userId}` | Zablokuj konto |
| POST | `/api/adminPanel/users/unlock-user/{userId}` | Odblokuj konto |
| GET | `/api/adminPanel/users/get-user-by-id` | Szczegóły użytkownika |
| POST | `/api/historyLog/get-history-logs` | Logi audytu (DataTable) |

### Inne

| Endpoint | Opis |
|----------|------|
| `GET /healthz` | Health check |
| `/swagger` | Dokumentacja API (tylko Development) |


## Autoryzacja

### Mechanizm

- **JWT Bearer Token** – token w nagłówku `Authorization: Bearer <token>`, ważność 24h
- **Google OAuth2** – przekierowanie do Google, po autoryzacji token JWT zwracany do frontendu
- **Role:** `Administrator`, `User`

### Uwagi

- Pierwsza zarejestrowana osoba automatycznie otrzymuje rolę `Administrator`
- Endpoint logowania jest objęty rate limitingiem (5 prób na minutę)
- SignalR wymaga tokenu JWT przekazanego jako query string: `?access_token=<token>`


## Real-time (SignalR)

### ChatHub – `/chatHub`

| Metoda (klient → serwer) | Opis |
|--------------------------|------|
| `SendMessageToGroup(groupId, content)` | Wyślij wiadomość do grupy |
| `SubscribeToMessages(groupId)` | Dołącz do grupy SignalR |
| `UnsubscribeFromMessages(groupId)` | Opuść grupę SignalR |
| `InviteUserToChat(groupId, targetUserId)` | Dodaj użytkownika do czatu |
| `LeaveConversation(groupId)` | Opuść rozmowę |

| Zdarzenie (serwer → klient) | Opis |
|-----------------------------|------|
| `ReceiveMessage(ChatMessageDto)` | Nowa wiadomość w grupie |
| `UserJoined(object)` | Użytkownik dołączył |
| `UserLeft(object)` | Użytkownik opuścił |
| `NewChatAssigned(groupId)` | Zaproszony użytkownik otrzymuje nowy czat |

### NotificationHub – `/notificationHub`

Przygotowany pod przyszłe powiadomienia (np. zaproszenia do znajomych).
