# Google Business Profile API — Setup Guide

This document covers everything you need to do on the Google side before the GBP integration
in Zambiq can be activated. These are manual steps performed by the account owner / developer.

---

## 1. Create a Google Cloud project

1. Go to [console.cloud.google.com](https://console.cloud.google.com)
2. Create a new project (e.g. **Zambiq Production**)
3. Note the **Project ID**

---

## 2. Request GBP API access

Google requires per-project manual approval before any quota is granted.

1. Fill in the form at: <https://support.google.com/business/contact/api_default>
2. Sign in as the **GBP owner** (not a manager — manager submissions are auto-rejected)
3. The business profile must be:
   - Verified on Google (claimed and accepted)
   - At least **60 days old** (newer profiles are auto-rejected)
4. Select all APIs you need access to (see list below)
5. Describe your use case: *"Internal restaurant management system to sync opening hours,
   read and reply to reviews, and manage business information from a single dashboard."*
6. Approval typically takes **3–10 business days**
7. After approval, your project's QPM quota changes from 0 to ~300 QPM — this is confirmation

---

## 3. Enable APIs in Google Cloud Console

Go to **APIs & Services > Library** and enable all eight APIs:

| API name | Purpose |
|---|---|
| My Business Account Management API | List accounts and locations |
| My Business Business Information API | Update hours, attributes |
| My Business Notifications API | Pub/Sub push notifications (optional) |
| Business Profile Performance API | Insights and metrics (future) |
| My Business Place Actions API | Place actions (future) |
| My Business Q&A API | Q&A management (future) |
| My Business Verifications API | Location verification |
| My Business Lodging API | Lodging attributes (not required for restaurants) |

You **must** also enable:
- **Google OAuth2 API** (for token introspection)

---

## 4. Configure the OAuth 2.0 consent screen

1. Go to **APIs & Services > OAuth consent screen**
2. Choose **External** (unless your Google Workspace org owns the domain)
3. Fill in:
   - App name: **Zambiq**
   - User support email: your email
   - Developer contact: your email
4. Add scopes:
   - `https://www.googleapis.com/auth/business.manage`
5. Add test users (your own Google account that manages the GBP listing)
6. Submit for verification when ready for production (required for >100 users or non-test mode)

> **Dev tip:** While the app is in *Testing* mode, only explicitly listed test users can
> complete the OAuth flow. Add yourself and any owner accounts used during testing.

---

## 5. Create OAuth 2.0 credentials

1. Go to **APIs & Services > Credentials**
2. Click **Create credentials > OAuth client ID**
3. Application type: **Web application**
4. Name: **Zambiq Web**
5. Authorized redirect URIs:
   - Local dev: `http://localhost:5001/gbp-callback`
   - Production: `https://yourdomain.com/gbp-callback`
6. Copy the **Client ID** and **Client Secret**

---

## 6. Configure Zambiq

Add the credentials to your environment:

```bash
# .env (never commit this file)
GoogleBusiness__Enabled=true
GoogleBusiness__ClientId=YOUR_CLIENT_ID.apps.googleusercontent.com
GoogleBusiness__ClientSecret=YOUR_CLIENT_SECRET
GoogleBusiness__RedirectUrl=http://localhost:5001/gbp-callback
```

Or in `appsettings.Development.json`:
```json
{
  "GoogleBusiness": {
    "Enabled": true,
    "ClientId": "...",
    "ClientSecret": "...",
    "RedirectUrl": "http://localhost:5001/gbp-callback"
  }
}
```

> **Keep `GoogleBusiness__Enabled=false`** until API access is approved. With `false`, all
> GBP endpoints return 404 and the nav item is hidden.

---

## 7. Apply the database migration

```bash
docker compose up -d booking-api
```

The `AddGoogleBusinessProfile` migration runs automatically on startup and creates:
- `GoogleBusinessConnections` table
- `GoogleReviews` table

---

## 8. Connect in the UI

1. Log in as Owner
2. Go to **Instellingen > Google Business** (`/admin/google-business`)
3. Click **Verbinden met Google Business Profile**
4. Complete the OAuth consent flow with the Google account that manages the GBP listing
5. Select the correct location when prompted
6. The status card shows **Verbonden** — opening hours will now sync automatically

---

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Controller returns 404 | `GoogleBusiness__Enabled` is `false` |
| "ClientId ontbreekt" error | `GoogleBusiness__ClientId` not set |
| OAuth redirect_uri_mismatch | Redirect URI in GCP credentials doesn't match `GoogleBusiness__RedirectUrl` |
| Empty location list after OAuth | Google account has no GBP listings, or wrong account used |
| QPM quota = 0 | API access not yet approved — wait or re-submit form |
| Token refresh fails → ReconnectRequired | Refresh token revoked (user revoked access in Google account settings) |
