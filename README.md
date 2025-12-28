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

## Integrating @funtime/ui in Consumer Sites

### Step 1: Install the Package

```bash
npm install @funtime/ui
# or
yarn add @funtime/ui
```

### Step 2: Initialize the Client (Required)

Create a file like `src/lib/funtime.ts` and initialize early in your app (e.g., in `_app.tsx` or `main.tsx`):

```typescript
import { initFuntimeClient } from '@funtime/ui';

// Call this BEFORE using any hooks or API calls
initFuntimeClient({
  baseUrl: process.env.NEXT_PUBLIC_IDENTITY_API_URL || 'http://localhost:5000',
  getToken: () => {
    if (typeof window !== 'undefined') {
      return localStorage.getItem('funtime_token');
    }
    return null;
  },
  onUnauthorized: () => {
    localStorage.removeItem('funtime_token');
    window.location.href = '/login';
  },
});
```

### Step 3: Use Authentication Hooks

```typescript
import { useAuth } from '@funtime/ui';

function MyComponent() {
  const {
    isAuthenticated,
    isLoading,
    user,           // { id, email, phoneNumber, systemRole }
    login,          // (email, password) => Promise
    loginWithOtp,   // (phone, otp) => Promise
    register,       // (email, password) => Promise
    logout,         // () => void
    sendOtp,        // (phone) => Promise
  } = useAuth();

  if (isLoading) return <div>Loading...</div>;

  return isAuthenticated ? (
    <div>Welcome, {user?.email}</div>
  ) : (
    <div>Please log in</div>
  );
}
```

### Step 4: Use Sites Hook (for site memberships)

```typescript
import { useSites } from '@funtime/ui';

function SiteMembership() {
  const {
    sites,          // User's site memberships
    isLoading,
    joinSite,       // (siteKey) => Promise
    leaveSite,      // (siteKey) => Promise
    checkMembership // (siteKey) => Promise<boolean>
  } = useSites();

  return (
    <div>
      {sites.map(site => (
        <div key={site.siteKey}>
          {site.siteKey} - Role: {site.role}
        </div>
      ))}
    </div>
  );
}
```

### Step 5: Use Payments Hook (for Stripe integration)

```typescript
import { usePayments } from '@funtime/ui';

function PaymentSection() {
  const {
    customer,           // Stripe customer info
    paymentMethods,     // Saved payment methods
    subscriptions,      // Active subscriptions
    isLoading,
    createPayment,      // (amountCents, description, siteKey?) => Promise
    addPaymentMethod,   // (stripePaymentMethodId) => Promise
    removePaymentMethod,// (paymentMethodId) => Promise
    createSubscription, // (stripePriceId, siteKey?) => Promise
    cancelSubscription, // (subscriptionId) => Promise
  } = usePayments();

  return (
    <div>
      {paymentMethods.map(pm => (
        <div key={pm.id}>
          {pm.brand} •••• {pm.last4}
        </div>
      ))}
    </div>
  );
}
```

### Step 6: Use Shared Components

```typescript
import {
  Button,       // Primary button component
  Input,        // Form input with validation
  Card,         // Container card
  AuthForm,     // Complete login/register form
  Avatar,       // User avatar with fallback
  SkillBadge,   // Skill level display (1.0 - 5.5+)
  SiteBadge,    // Site membership badge
} from '@funtime/ui';

function MyPage() {
  return (
    <Card>
      <Avatar src={user.avatarUrl} name={user.displayName} size="lg" />
      <SkillBadge level={user.skillLevel} />
      <Button variant="primary" onClick={handleClick}>
        Click Me
      </Button>
    </Card>
  );
}
```

### Direct API Access

For custom API calls not covered by hooks:

```typescript
import { funtimeApi } from '@funtime/ui';

// Auth endpoints
const user = await funtimeApi.auth.me();
const result = await funtimeApi.auth.login(email, password);
await funtimeApi.auth.register(email, password);
await funtimeApi.auth.sendOtp(phone);
await funtimeApi.auth.verifyOtp(phone, otp);
await funtimeApi.auth.changePassword(currentPassword, newPassword);

// Profile endpoints
const profile = await funtimeApi.profile.get();
await funtimeApi.profile.update({ displayName, bio, city });
const publicProfile = await funtimeApi.profile.getPublic(userId);

// Sites endpoints
const sites = await funtimeApi.sites.list();
await funtimeApi.sites.join(siteKey);
await funtimeApi.sites.leave(siteKey);

// Payments endpoints
const customer = await funtimeApi.payments.getCustomer();
const methods = await funtimeApi.payments.getPaymentMethods();
const history = await funtimeApi.payments.getHistory();
```

### Environment Variables for Consumer Sites

```env
# .env.local or .env
NEXT_PUBLIC_IDENTITY_API_URL=http://localhost:5000
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_xxx
```

### Example: Next.js Integration

```typescript
// pages/_app.tsx
import '@funtime/ui/styles.css';  // Import shared styles
import { initFuntimeClient } from '@funtime/ui';

// Initialize before app renders
initFuntimeClient({
  baseUrl: process.env.NEXT_PUBLIC_IDENTITY_API_URL!,
  getToken: () => localStorage.getItem('funtime_token'),
  onUnauthorized: () => {
    localStorage.removeItem('funtime_token');
    window.location.href = '/login';
  },
});

export default function App({ Component, pageProps }) {
  return <Component {...pageProps} />;
}
```

```typescript
// pages/login.tsx
import { AuthForm } from '@funtime/ui';
import { useRouter } from 'next/router';

export default function LoginPage() {
  const router = useRouter();

  const handleSuccess = (token: string) => {
    localStorage.setItem('funtime_token', token);
    router.push('/dashboard');
  };

  return (
    <AuthForm
      mode="login"
      onSuccess={handleSuccess}
      showOtpOption={true}
      siteKey="community"  // Auto-join this site on register
    />
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
