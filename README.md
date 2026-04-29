# ŠišApp

Seminar project for **Software Development II (Razvoj softvera II)**.

## Quick Start

.

1. Download `fit-build-2026-04-29.zip` from GitHub `Releases`.
2. Extract package.
3. Start backend with Docker.
4. Start client applications from extracted package.
5. Run booking and payment test flow.

---

## Setup

### 1) Download build package

1. Open project GitHub repository.
2. Go to `Releases` section.
3. Download `fit-build-2026-04-29.zip`.
4. Extract zip locally.

### 2) Backend setup

1. In project root, keep `.env-tajne.zip` (replacement for `.env`).
2. Extract `.env-tajne.zip` in same folder to get `.env`.
3. In project root, run:

   ```bash
   docker compose up --build
   ```

### 3) Client applications setup

1. Desktop: run `sisapp_desktop.exe` from `Release` folder.
2. Mobile: if app already installed on emulator, uninstall first.
3. Mobile: drag `.apk` from `flutter-apk` folder to Android emulator.
4. Wait for install, then launch app.
5. Log in with matching test user (credentials below).

---

## Test Scenario

Main business flow:

**Admin manages salons/barbers (Desktop) -> Barber configures services and schedule (Mobile) -> Customer books and pays for appointment (Mobile)**

### Step 1: Admin (Desktop)

1. Log in as `desktop`.
2. Create/edit salons, barbers, and services.

### Step 2: Barber (Mobile)

1. Log in as `barber`.
2. Set salon profile and available services.
3. Configure working schedule via **Calendar**.

### Step 3: Customer (Mobile)

1. Log in as `mobile` (use second device/emulator, or log out first).
2. Browse salons and barbers.
3. Select service and available time slot.
4. Click **Book Appointment**.
5. Pay with Stripe test card or PayPal sandbox account.

---

## Test Credentials

| Platform | Role | Username | Password |
|----------|------|----------|----------|
| Desktop  | Admin | `desktop` | `test` |
| Mobile   | Barber | `barber` | `test` |
| Mobile   | Barber | `barber1` | `test` |
| Mobile   | Customer | `mobile` | `test` |

### Stripe Test Card

| Field | Value |
|-------|-------|
| Card Number | `4242 4242 4242 4242` |
| Expiry | `12/34` |
| CVC | `123` |

### PayPal Sandbox Credentials

| Field | Value |
|-------|-------|
| Email | `sb-iborh50894693@personal.example.com` |
| Password | `Zd1nX;_b` |

---

## Technology Stack

| Component | Technologies |
|-----------|--------------|
| Backend | ASP.NET Core, SQL Server, RabbitMQ, Docker |
| Frontend | Flutter (Mobile and Desktop) |
| Integrations | Stripe (payments), PayPal (payments) |
| Recommender | Content-based filtering recommendation system |
