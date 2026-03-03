# ŠišApp
Seminar paper for the course **Software Development II (Razvoj softvera II)**

## Setup Instructions

> **Password for .zip files is `fit`**

---

### Backend Setup
1. Extract `fit-build-2026-03-03-env`
2. Place the `.env` file into the project root directory 
3. Open the project root directory in terminal and run:
   ```bash
   docker compose up --build
   ```

---

### Desktop Application Setup
1. Extract `fit-build-2026-03-03-desktop`
2. Run `sisapp_desktop.exe` from the "Release" folder
3. Login with admin credentials (see below)

---

### Mobile Application Setup
1. Uninstall the app from Android emulator if it already exists
2. Extract `fit-build-2026-03-03-mobile`
3. Drag the `.apk` file from "flutter-apk" folder to the emulator
4. Wait for installation to complete
5. Launch the app and login with credentials (see below)

---

## Testing Guide

### Main Testing Flow
The core functionality follows this flow:

**Admin manages salons/barbers (Desktop) → Barber sets up services (Mobile) → Customer books and pays for an appointment (Mobile)**

---

#### Step 1: Admin Setup (Desktop)
1. Login as **admin**
2. Manage salons, barbers, and services through the admin panel

#### Step 2: Barber Setup (Mobile)
1. Login as **barber**
2. Set up your salon profile and available services
3. Configure your working schedule via **"Calendar"**

#### Step 3: Customer Books Appointment (Mobile)
1. Login as **mobile** (use a different device/emulator or logout first)
2. Browse available salons and barbers
3. Select a service and available time slot
4. Click **"Book Appointment"**
5. Complete payment with the test card

---

## Credentials

| Platform | Role | Username | Password |
|----------|------|----------|----------|
| Desktop | Admin | `desktop` | `test` |
| Mobile | Barber | `barber` | `test` |
| Mobile | mobile | `mobile` | `test` |

### Stripe Test Card

| Field | Value |
|-------|-------|
| Card Number | `4242 4242 4242 4242` |
| Expiry | `12/34` |
| CVC | `123` |
| ZIP | `12345` |

---

## Creating New Users

When creating new users, the password must meet the following requirements:
- At least one **uppercase letter** (A-Z)
- At least one **lowercase letter** (a-z)
- At least one **digit** (0-9)
- At least one **special character** (!@#$%^&* etc.)

> **Suggested test password:** `Test123!`



## Technology Stack

| Component | Technologies |
|-----------|-------------|
| Backend | ASP.NET Core, SQL Server, RabbitMQ, Docker |
| Frontend | Flutter (Mobile & Desktop) |
| Integrations | Stripe (payments), PayPal (payments) |
| Recommender | Content-based filtering recommendation system |
