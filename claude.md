# FTPB Auth Service

## Project Overview

FTPB Auth Service is a .NET 8.0 authentication backend that supports:
- Email/password registration and login
- Phone/OTP login via Twilio SMS
- Account linking (add phone to email account or email to phone account)
- JWT token-based authentication
- Rate limiting for OTP requests

## Project Structure

```
backend/
├── FTPBAuth.sln              # Solution file
├── FTPBAuth.API/             # Main API project
│   ├── Controllers/          # API controllers
│   │   └── AuthController.cs # All auth endpoints
│   ├── Data/                 # Database context
│   │   └── ApplicationDbContext.cs
│   ├── DTOs/                 # Data transfer objects
│   │   └── AuthDTOs.cs
│   ├── Models/               # Entity models
│   │   ├── User.cs
│   │   ├── OtpRequest.cs
│   │   └── OtpRateLimit.cs
│   ├── Services/             # Business logic services
│   │   ├── IJwtService.cs
│   │   ├── JwtService.cs
│   │   ├── IOtpService.cs
│   │   ├── OtpService.cs
│   │   ├── ISmsService.cs
│   │   └── TwilioSmsService.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── FTPBAuth.API.csproj
└── SQL-Scripts/              # Database scripts
    ├── 001_CreateDatabase.sql
    ├── 002_CreateTables.sql
    ├── 003_CleanupExpiredOtps.sql
    └── 004_SeedTestData.sql
```

## API Endpoints

| Method | Endpoint | Auth Required | Description |
|--------|----------|---------------|-------------|
| POST | `/auth/register` | No | Register with email/password |
| POST | `/auth/login` | No | Login with email/password |
| POST | `/auth/otp/send` | No | Send OTP to phone number |
| POST | `/auth/otp/verify` | No | Verify OTP and login/register |
| POST | `/auth/link-phone` | Yes | Link phone to email account |
| POST | `/auth/link-email` | Yes | Link email to phone account |
| GET | `/auth/me` | Yes | Get current user info |
| POST | `/auth/validate` | No | Validate a JWT token |

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB, Express, or full version)
- Twilio account (for SMS/OTP functionality)

### Database Setup

1. Run SQL scripts in order:
   ```bash
   # Connect to SQL Server and run:
   sqlcmd -S localhost -i backend/SQL-Scripts/001_CreateDatabase.sql
   sqlcmd -S localhost -d FTPBAuth -i backend/SQL-Scripts/002_CreateTables.sql
   ```

2. Or let EF Core handle migrations (auto-applies in Development):
   ```bash
   cd backend/FTPBAuth.API
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

### Configuration

1. Update `appsettings.json` with your settings:
   - **ConnectionStrings:DefaultConnection** - SQL Server connection string
   - **Jwt:Key** - Secret key for JWT signing (min 32 characters)
   - **Jwt:Issuer** - JWT issuer name
   - **Jwt:Audience** - JWT audience name
   - **Twilio:AccountSid** - Your Twilio Account SID
   - **Twilio:AuthToken** - Your Twilio Auth Token
   - **Twilio:PhoneNumber** - Your Twilio phone number (for sending SMS)

2. For local development, use user secrets:
   ```bash
   cd backend/FTPBAuth.API
   dotnet user-secrets init
   dotnet user-secrets set "Twilio:AccountSid" "your-sid"
   dotnet user-secrets set "Twilio:AuthToken" "your-token"
   ```

### Running the API

```bash
cd backend/FTPBAuth.API
dotnet restore
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: http://localhost:5000/swagger

## Request/Response Examples

### Register
```json
POST /auth/register
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}

Response:
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "message": "Registration successful.",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "phoneNumber": null,
    "isEmailVerified": false,
    "isPhoneVerified": false,
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

### Login (Email/Password)
```json
POST /auth/login
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

### Send OTP
```json
POST /auth/otp/send
{
  "phoneNumber": "+1234567890"
}

Response:
{
  "success": true,
  "message": "OTP sent successfully."
}
```

### Verify OTP
```json
POST /auth/otp/verify
{
  "phoneNumber": "+1234567890",
  "code": "123456"
}
```

### Link Phone (Requires Auth)
```json
POST /auth/link-phone
Authorization: Bearer <token>
{
  "phoneNumber": "+1234567890",
  "code": "123456"  // OTP code received via SMS
}
```

### Link Email (Requires Auth)
```json
POST /auth/link-email
Authorization: Bearer <token>
{
  "email": "user@example.com",
  "password": "NewPassword123!"
}
```

### Get Current User (Requires Auth)
```json
GET /auth/me
Authorization: Bearer <token>
```

### Validate Token
```json
POST /auth/validate
{
  "token": "eyJhbGciOiJIUzI1NiIs..."
}

Response:
{
  "valid": true,
  "userId": 1,
  "email": "user@example.com",
  "phoneNumber": "+1234567890",
  "message": "Token is valid."
}
```

## Rate Limiting

OTP requests are rate-limited to prevent abuse:
- Default: 5 requests per 15-minute window
- After exceeding the limit, the phone number is blocked until the window expires
- Configure in `appsettings.json`:
  ```json
  "RateLimiting": {
    "OtpMaxAttempts": 5,
    "OtpWindowMinutes": 15
  }
  ```

## Security Notes

1. **JWT Secret Key**: Use a strong, random key (min 32 characters) in production
2. **HTTPS**: Always use HTTPS in production
3. **Twilio Credentials**: Never commit Twilio credentials; use environment variables or user secrets
4. **Password Hashing**: Passwords are hashed using BCrypt
5. **Rate Limiting**: OTP rate limiting prevents SMS bombing attacks

## Development Commands

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Run with watch (hot reload)
dotnet watch run

# Run tests (when added)
dotnet test
```

## Maintenance

Run the cleanup script periodically to remove expired OTP records:
```sql
-- backend/SQL-Scripts/003_CleanupExpiredOtps.sql
```

Consider setting up a SQL Server Agent job or scheduled task to run this script daily.
