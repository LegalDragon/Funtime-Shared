# Auth-UI Integration Guide

Shared authentication UI for the Funtime Pickleball platform. This UI serves multiple sites (pickleball.community, pickleball.college, pickleball.date, pickleball.jobs) with site-specific branding.

## Production URLs

| Service | URL |
|---------|-----|
| **Auth UI** | https://shared.funtimepb.com |
| **API** | https://shared.funtimepb.com/api |

Example login link for production:
```
https://shared.funtimepb.com/login?site=community&redirect=https://pickleball.community/auth/callback
```

## Quick Start for External Sites

### 1. Link to Auth-UI

```html
<!-- Login link -->
<a href="https://shared.funtimepb.com/login?site=community&redirect=https://pickleball.community/auth/callback">
  Login
</a>

<!-- Register link -->
<a href="https://shared.funtimepb.com/register?site=community&redirect=https://pickleball.community/auth/callback">
  Sign Up
</a>
```

### 2. Handle the Callback

After successful auth, users are redirected to your callback URL with a token:

```
https://pickleball.community/auth/callback?token=eyJhbG...
```

Your callback handler should:
1. Extract the token from URL
2. Store it (session/cookie/localStorage)
3. Use it for API requests

```javascript
// Example callback handler
const urlParams = new URLSearchParams(window.location.search);
const token = urlParams.get('token');

if (token) {
  // Store token
  localStorage.setItem('auth_token', token);

  // Redirect to dashboard or home
  window.location.href = '/dashboard';
}
```

### 3. Make Authenticated API Requests

```javascript
fetch('https://shared.funtimepb.com/api/user/profile', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});
```

---

## URL Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `site` | Site key for branding | `?site=community` |
| `redirect` | Callback URL after auth | `?redirect=https://pickleball.community/auth/callback` |
| `returnTo` | Path to return to after callback | `?returnTo=/dashboard` |

**Full Example:**
```
/login?site=community&redirect=https://pickleball.community/auth/callback&returnTo=/dashboard
```

---

## Available Routes

| Route | Purpose | Public |
|-------|---------|--------|
| `/login` | Email/Phone login (password or OTP) | Yes |
| `/register` | New account creation | Yes |
| `/forgot-password` | Password reset + quick account creation | Yes |
| `/sites` | Site selection after login | No |
| `/admin` | Admin dashboard (SU role only) | No |
| `/terms-of-service` | Terms of Service display | Yes |
| `/privacy-policy` | Privacy Policy display | Yes |

---

## Authentication Flows

### Email + Password
```
1. User enters email + password
2. POST /auth/login { email, password }
3. Response: { token, user }
4. Redirect to callback with token
```

### Phone + OTP
```
1. User enters phone number
2. POST /auth/otp/send { phoneNumber }
3. SMS code sent
4. User enters 6-digit code
5. POST /auth/otp/verify { phoneNumber, code }
6. Response: { token, user }
7. Redirect to callback with token
```

### Phone + Password
```
1. User enters phone + password
2. POST /auth/login/phone { phoneNumber, password }
3. Response: { token, user }
4. Redirect to callback with token
```

### Password Reset
```
1. User enters email or phone
2. POST /auth/password-reset/send { email } or { phoneNumber }
3. Code sent via email/SMS
4. POST /auth/password-reset/verify { email/phoneNumber, code }
5. Response includes: { accountExists: true/false }
6a. If account exists: POST /auth/password-reset/complete { ..., newPassword }
6b. If no account: POST /auth/password-reset/register { ..., password } (creates new account)
```

---

## Runtime Configuration

The UI uses `/config.js` for runtime configuration:

```javascript
// /config.js (served at web root)
window.__FUNTIME_CONFIG__ = {
  API_URL: "https://shared.funtimepb.com/api",
  STRIPE_PUBLISHABLE_KEY: "pk_live_..."
};
```

**For development:** Use `.env` file with `VITE_API_URL` and `VITE_STRIPE_PUBLISHABLE_KEY`

---

## API Endpoints

### Public Auth Endpoints

```
GET  /auth/sites                    - List public sites
POST /auth/login                    - Email login
POST /auth/login/phone              - Phone login with password
POST /auth/register                 - Email registration
POST /auth/otp/send                 - Send OTP to phone
POST /auth/otp/verify               - Verify OTP code
POST /auth/password-reset/send      - Request password reset
POST /auth/password-reset/verify    - Verify reset code
POST /auth/password-reset/complete  - Complete password reset
POST /auth/password-reset/register  - Quick register (from reset flow)
```

### Settings Endpoints (Public)

```
GET /settings/logo                  - Get main logo info
GET /settings/logo-url?site={key}   - Get logo URL by site key (see below)
GET /settings/logo-overlay?site={key} - Get both main and site logos (see below)
GET /settings/terms-of-service      - Get Terms of Service
GET /settings/privacy-policy        - Get Privacy Policy
```

#### Logo URL Endpoint

Simple endpoint to get the logo URL for a site:

```
GET /settings/logo-url              → Returns main logo URL
GET /settings/logo-url?site=community → Returns community site's logo URL
GET /settings/logo-url?site=pickleball.community → Also works (prefix stripped)
```

Response:
```json
{
  "logoUrl": "/asset/5"
}
```

#### Logo Overlay Endpoint

Get both main logo and site logo for overlay display:

```
GET /settings/logo-overlay              → Returns main logo only
GET /settings/logo-overlay?site=community → Returns both main and site logos
```

Response:
```json
{
  "mainLogoUrl": "/asset/3",
  "siteLogoUrl": "/asset/5",
  "siteName": "Community"
}
```

Use this to build full URLs: `https://shared.funtimepb.com/api/asset/3`

### Admin Endpoints (Require Bearer Token)

```
GET    /admin/stats                 - Dashboard statistics
GET    /admin/sites                 - List all sites
POST   /admin/sites                 - Create site
PUT    /admin/sites/{key}           - Update site
POST   /admin/sites/{key}/logo      - Upload site logo
DELETE /admin/sites/{key}/logo      - Delete site logo
GET    /admin/users                 - Search users
GET    /admin/users/{id}            - User details
PUT    /admin/users/{id}            - Update user
GET    /admin/payments              - Payment history
POST   /admin/payments/charge       - Create payment intent
```

---

## Site Branding

### Site Key Matching

Site keys are **case-insensitive**. The UI looks up the site in the database and displays:
- Main logo (shared across all sites)
- Site logo overlay (bottom-right of main logo)
- Title: `Pickleball.{SiteName}` or `Funtime Pickleball` (fallback)

### Logo Configuration

Upload via Admin Dashboard (`/admin` > Settings tab):
1. **Main Logo** - Shared branding, appears on all auth pages
2. **Site Logo** - Per-site branding, overlays the main logo

---

## Allowed Redirect Domains

For security, only these domains can receive auth callbacks:

```
pickleball.community
pickleball.college
pickleball.date
pickleball.jobs
localhost
127.0.0.1
```

To add domains, update `frontend/auth-ui/src/utils/redirect.ts`.

---

## Token Format

The auth token is a JWT containing:
- User ID
- Email/Phone
- System Role (e.g., `SU` for super user)
- Expiration

**Storage:** `localStorage.auth_token`

**Header format:** `Authorization: Bearer {token}`

---

## Building & Deployment

### Development
```bash
cd frontend/auth-ui
npm install
npm run dev
```

### Production Build
```bash
npm run build
```

Output in `dist/` folder. Deploy to web server and configure `/config.js`.

### Environment Variables

| Variable | Purpose |
|----------|---------|
| `VITE_API_URL` | API base URL (dev fallback) |
| `VITE_STRIPE_PUBLISHABLE_KEY` | Stripe public key (dev fallback) |

---

## Integration Checklist

```
[ ] Register your site in Admin Dashboard (/admin > Sites)
[ ] Upload site logo for branding
[ ] Add login/register links with ?site={key}&redirect={callback}
[ ] Implement callback handler to extract token
[ ] Store token and use for authenticated requests
[ ] (Optional) Add Terms of Service and Privacy Policy content
```

---

## Admin Dashboard Features

Access at `/admin` (requires SU system role):

| Tab | Features |
|-----|----------|
| **Overview** | User stats, revenue, subscriptions |
| **Sites** | Create/edit sites, upload logos |
| **Users** | Search, view details, manage users |
| **Payments** | Payment history, manual charges |
| **Notifications** | Email profiles, templates, queue |
| **Settings** | Main logo, Terms of Service, Privacy Policy |

---

## Tech Stack

- **React 19** with TypeScript
- **Vite** for building
- **Tailwind CSS** for styling
- **React Router** for navigation
- **Stripe** for payments
- **Lucide** for icons

---

# Backend Reference

This section documents the backend API for sites that need to validate tokens or access user data.

## JWT Token Structure

Tokens are signed with HMAC-SHA256. Claims included:

| Claim | Type | Description |
|-------|------|-------------|
| `nameid` | string | User ID (integer as string) |
| `email` | string | User's email (if set) |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone` | string | Phone number (if set) |
| `role` | string | System role (`SU` for super admin, null for regular) |
| `sites` | JSON array | List of site keys user belongs to: `["community","college"]` |
| `jti` | string | Unique token ID |
| `exp` | number | Expiration timestamp |
| `iss` | string | Issuer: `FuntimePickleball` |
| `aud` | string | Audience: `FuntimePickleballUsers` |

### Example Decoded Token
```json
{
  "nameid": "42",
  "email": "user@example.com",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone": "+15551234567",
  "role": "SU",
  "sites": "[\"community\",\"college\"]",
  "jti": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "exp": 1735430400,
  "iss": "FuntimePickleball",
  "aud": "FuntimePickleballUsers"
}
```

---

## JWT Configuration

Backend uses these settings in `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "your-32-char-secret-key-here!!!",
    "Issuer": "FuntimePickleball",
    "Audience": "FuntimePickleballUsers",
    "ExpirationInMinutes": 1440
  }
}
```

**Important:** All sites sharing auth must use the same JWT Key, Issuer, and Audience.

---

## Token Validation (C# Example)

For ASP.NET Core sites consuming the token:

```csharp
// Program.cs or Startup.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = "FuntimePickleball",
            ValidateAudience = true,
            ValidAudience = "FuntimePickleballUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// In controllers, access claims:
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var phone = User.FindFirst(ClaimTypes.MobilePhone)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
var sitesJson = User.FindFirst("sites")?.Value;
var sites = sitesJson != null ? JsonSerializer.Deserialize<List<string>>(sitesJson) : null;
```

---

## Token Validation (Node.js Example)

```javascript
const jwt = require('jsonwebtoken');

const JWT_SECRET = 'your-32-char-secret-key-here!!!';
const JWT_ISSUER = 'FuntimePickleball';
const JWT_AUDIENCE = 'FuntimePickleballUsers';

function validateToken(token) {
  try {
    const decoded = jwt.verify(token, JWT_SECRET, {
      issuer: JWT_ISSUER,
      audience: JWT_AUDIENCE
    });

    return {
      valid: true,
      userId: parseInt(decoded.nameid),
      email: decoded.email,
      phone: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone'],
      role: decoded.role,
      sites: decoded.sites ? JSON.parse(decoded.sites) : []
    };
  } catch (err) {
    return { valid: false, error: err.message };
  }
}

// Express middleware example
function authMiddleware(req, res, next) {
  const authHeader = req.headers.authorization;
  if (!authHeader?.startsWith('Bearer ')) {
    return res.status(401).json({ error: 'No token provided' });
  }

  const token = authHeader.substring(7);
  const result = validateToken(token);

  if (!result.valid) {
    return res.status(401).json({ error: 'Invalid token' });
  }

  req.user = result;
  next();
}
```

---

## Database Schema

### Users Table
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Email NVARCHAR(255) NULL,
    PasswordHash NVARCHAR(255) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    SystemRole NVARCHAR(10) NULL,        -- 'SU' for super admin
    IsEmailVerified BIT DEFAULT 0,
    IsPhoneVerified BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    LastLoginAt DATETIME2 NULL
);
```

### Sites Table
```sql
CREATE TABLE Sites (
    Key NVARCHAR(50) PRIMARY KEY,        -- e.g., 'community', 'college'
    Name NVARCHAR(100) NOT NULL,         -- e.g., 'Community', 'College'
    Description NVARCHAR(500) NULL,
    Url NVARCHAR(255) NULL,              -- e.g., 'https://pickleball.community'
    LogoUrl NVARCHAR(500) NULL,          -- Path to site logo
    IsActive BIT DEFAULT 1,
    RequiresSubscription BIT DEFAULT 0,
    MonthlyPriceCents BIGINT NULL,
    YearlyPriceCents BIGINT NULL,
    DisplayOrder INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);
```

### UserSites Table (Many-to-Many)
```sql
CREATE TABLE UserSites (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    SiteKey NVARCHAR(50) NOT NULL,       -- References Sites.Key
    JoinedAt DATETIME2 DEFAULT GETUTCDATE(),
    IsActive BIT DEFAULT 1,
    Role NVARCHAR(50) DEFAULT 'member'   -- 'member', 'admin', 'moderator'
);
```

### Settings Table
```sql
CREATE TABLE Settings (
    Id INT PRIMARY KEY IDENTITY,
    Key NVARCHAR(100) NOT NULL UNIQUE,   -- 'main_logo', 'terms_of_service', 'privacy_policy'
    Value NVARCHAR(MAX) NOT NULL,
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedBy INT NULL
);
```

---

## Site-Specific Data Access Pattern

When building a site (e.g., pickleball.community), check if user belongs to that site:

```csharp
// Check if user can access this site
public async Task<bool> CanAccessSite(int userId, string siteKey)
{
    return await _context.UserSites
        .AnyAsync(us => us.UserId == userId
                     && us.SiteKey == siteKey
                     && us.IsActive);
}

// Get user's role on a site
public async Task<string?> GetUserSiteRole(int userId, string siteKey)
{
    var userSite = await _context.UserSites
        .FirstOrDefaultAsync(us => us.UserId == userId && us.SiteKey == siteKey);
    return userSite?.Role;
}
```

Or check the `sites` claim in the JWT (faster, no DB call):
```csharp
var sitesJson = User.FindFirst("sites")?.Value;
var sites = JsonSerializer.Deserialize<List<string>>(sitesJson ?? "[]");
bool canAccess = sites.Contains("community");
```

---

## Backend Project Structure

```
backend/Funtime.Identity.Api/
├── Controllers/
│   ├── AuthController.cs        # Login, register, OTP, password reset
│   ├── AdminController.cs       # Admin stats, sites, users management
│   ├── SettingsController.cs    # Logo, ToS, Privacy Policy
│   └── AssetController.cs       # File uploads
├── Models/
│   ├── User.cs                  # User entity
│   ├── Site.cs                  # Site configuration
│   ├── UserSite.cs              # User-site membership
│   ├── Setting.cs               # Key-value settings
│   └── ...                      # Payment, Subscription, etc.
├── Services/
│   ├── JwtService.cs            # Token generation/validation
│   ├── OtpService.cs            # SMS/Email OTP
│   └── ...
├── Data/
│   └── ApplicationDbContext.cs  # EF Core context
└── appsettings.json             # Configuration
```

---

## Building a New Site

When creating a new pickleball.* site:

1. **Register site in admin**: Add via `/admin` > Sites tab
2. **Share JWT config**: Use same `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
3. **Add to allowed domains**: Update `frontend/auth-ui/src/utils/redirect.ts`
4. **Implement callback**: Handle `?token=...` on your auth callback route
5. **Validate tokens**: Use the validation code above
6. **Check site membership**: Verify user belongs to your site via `sites` claim or DB

### Callback Route Example (ASP.NET Core)
```csharp
[HttpGet("/auth/callback")]
public IActionResult AuthCallback([FromQuery] string token, [FromQuery] string? returnTo)
{
    var (isValid, userId, email, phone, role, sites) = _jwtService.ValidateToken(token);

    if (!isValid)
        return Redirect("/login?error=invalid_token");

    // Check if user belongs to this site
    if (sites == null || !sites.Contains("community"))
        return Redirect("/login?error=no_access");

    // Create session/cookie
    HttpContext.Session.SetInt32("UserId", userId.Value);
    HttpContext.Session.SetString("Email", email ?? "");

    return Redirect(returnTo ?? "/dashboard");
}
```

---

## API Response Formats

### Successful Auth Response
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 42,
    "email": "user@example.com",
    "phoneNumber": "+15551234567",
    "systemRole": null
  }
}
```

### Error Response
```json
{
  "success": false,
  "message": "Invalid credentials"
}
```

### Site List Response (GET /auth/sites)
```json
[
  {
    "key": "community",
    "name": "Community",
    "description": "Connect with pickleball players",
    "url": "https://pickleball.community",
    "logoUrl": "/asset/5"
  },
  {
    "key": "college",
    "name": "College",
    "description": "College pickleball leagues",
    "url": "https://pickleball.college",
    "logoUrl": "/asset/6"
  }
]
```
