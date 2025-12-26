# Funtime Pickleball - Shared Platform

Shared infrastructure for all pickleball.* sites by Funtime Pickleball Inc.

## Architecture Overview

This repository contains the shared services used across all Funtime Pickleball sites:

### Backend: Funtime.Identity.Api (.NET 8)
Location: `/backend/Funtime.Identity.Api`

A centralized authentication and payments API that provides:
- **Authentication**: Email/password, phone/OTP (via Twilio), and external OAuth providers
- **User Profiles**: Shared profile data across all sites
- **Site Memberships**: Track which sites users have joined
- **Payments**: Stripe integration for payments and subscriptions
- **JWT Tokens**: With `sites[]` claim for authorization across sites

### Frontend: @funtime/ui (React + TypeScript)
Location: `/funtime-ui`

A shared React component library published as an npm package:
- **API Client**: TypeScript client for all Identity API endpoints
- **Hooks**: `useAuth`, `useSites`, `usePayments`
- **Components**: Button, Input, Card, AuthForm, Avatar, SkillBadge, SiteBadge

## Database: FuntimeIdentity (MS SQL Server)

### Auth Tables
- **Users** - Core user accounts with INT UserId (identity)
- **OtpRequests** - Phone OTP tracking
- **OtpRateLimits** - Rate limiting for OTP requests
- **ExternalLogins** - OAuth provider links (Google, Apple, etc.)

### Profile Tables
- **UserProfiles** - Shared profile data (name, avatar, city, skill level, bio)
- **UserSites** - Site membership tracking with roles

### Payment Tables
- **PaymentCustomers** - Stripe customer records
- **PaymentMethods** - Stored payment methods (cards)
- **Payments** - Transaction history
- **Subscriptions** - Recurring subscription records

## Consumer Sites (Separate Repos)
- **pickleball.community** - Community features
- **pickleball.college** - Educational content
- **pickleball.date** - Dating/social matching
- **pickleball.jobs** - Job board

Each site has its own database but references UserId from the shared FuntimeIdentity database.

## Getting Started

### Backend Setup

```bash
cd backend/Funtime.Identity.Api
dotnet restore
# Update appsettings.json with your connection string and API keys
dotnet run
```

The API runs on:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: http://localhost:5000/swagger

### Frontend Setup

```bash
cd funtime-ui
npm install
npm run build
```

### Using @funtime/ui in a Site

```typescript
import { initFuntimeClient, useAuth, Button, AuthForm } from '@funtime/ui';

// Initialize the client
initFuntimeClient({
  baseUrl: 'https://identity.funtime.com',
  getToken: () => localStorage.getItem('funtime_token'),
  onUnauthorized: () => {
    localStorage.removeItem('funtime_token');
    window.location.href = '/login';
  },
});

// Use in components
function App() {
  const { isAuthenticated, user, login, logout } = useAuth();

  return (
    <div>
      {isAuthenticated ? (
        <Button onClick={logout}>Logout</Button>
      ) : (
        <AuthForm mode="login" onSubmit={data => login(data.email!, data.password!)} />
      )}
    </div>
  );
}
```

## API Endpoints

### Authentication (`/auth`)
- `POST /auth/register` - Email/password registration
- `POST /auth/login` - Email/password login
- `POST /auth/otp/send` - Send OTP to phone
- `POST /auth/otp/verify` - Verify OTP and login/register
- `POST /auth/validate` - Validate JWT token (includes sites[])
- `GET /auth/me` - Get current user
- `POST /auth/link-phone` - Link phone to account
- `POST /auth/link-email` - Link email to account
- `POST /auth/external-login` - OAuth provider login
- `POST /auth/change-password` - Change password
- `POST /auth/reset-password` - Reset password via OTP

### Profile (`/profile`)
- `GET /profile` - Get current user's profile
- `PUT /profile` - Update profile
- `GET /profile/full` - Get full user info with sites
- `GET /profile/{userId}` - Get public profile

### Sites (`/sites`)
- `GET /sites` - Get user's site memberships
- `POST /sites/join` - Join a site
- `POST /sites/leave` - Leave a site
- `GET /sites/check/{siteKey}` - Check membership

### Payments (`/payments`)
- `GET /payments/customer` - Get/create Stripe customer
- `POST /payments/setup-intent` - Create setup intent for adding cards
- `GET /payments/payment-methods` - List payment methods
- `POST /payments/payment-methods` - Attach payment method
- `DELETE /payments/payment-methods/{id}` - Remove payment method
- `POST /payments/create-payment` - Create payment intent
- `GET /payments/history` - Payment history
- `GET /payments/subscriptions` - List subscriptions
- `POST /payments/subscriptions` - Create subscription
- `POST /payments/subscriptions/cancel` - Cancel subscription
- `POST /payments/webhook` - Stripe webhook endpoint

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FuntimeIdentity;..."
  },
  "Jwt": {
    "Key": "your-secret-key-at-least-32-chars",
    "Issuer": "FuntimeIdentity",
    "Audience": "FuntimePickleballUsers",
    "ExpirationInMinutes": 60
  },
  "Stripe": {
    "SecretKey": "sk_...",
    "PublishableKey": "pk_...",
    "WebhookSecret": "whsec_..."
  },
  "Twilio": {
    "AccountSid": "AC...",
    "AuthToken": "...",
    "PhoneNumber": "+1..."
  }
}
```

## Key Design Decisions

1. **Integer UserIds**: For backward compatibility with legacy systems
2. **JWT with sites[] claim**: Each token includes the user's active site memberships
3. **Stripe for payments**: Full payment and subscription support
4. **Shared profiles**: Common profile data across all sites
5. **Site-specific data**: Each site maintains its own database for domain-specific data

## SQL Scripts

Run in order:
1. `001_CreateDatabase.sql` - Create FuntimeIdentity database
2. `002_CreateTables.sql` - Create auth tables
3. `005_CreateExternalLoginsTable.sql` - External OAuth logins
4. `006_CreateProfileAndSiteTables.sql` - User profiles and site memberships
5. `007_CreatePaymentTables.sql` - Stripe payment tables

## License

Proprietary - Funtime Pickleball Inc.
