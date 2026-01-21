# API Key Authentication Guide

This document describes how to authenticate API requests using API keys for trusted partner applications.

## Overview

API keys provide a simple way for trusted applications to authenticate with the Funtime Identity API without user credentials. Each key is scoped to specific permissions and can be restricted by IP address.

Most endpoints support **dual authentication** - you can use either an API key OR a JWT token. API key takes precedence if both are provided.

## Getting an API Key

Contact your system administrator to obtain an API key. You will receive:
- **API Key**: A 64-character string (e.g., `pk_comm_a1b2c3d4e5f6...`)
- **Allowed Scopes**: The permissions granted to your key

Keep your API key secure. Do not expose it in client-side code or public repositories.

## Authentication

Include your API key in the `X-Api-Key` header with every request:

```
X-Api-Key: your_api_key_here
```

### Example Requests

**cURL:**
```bash
curl -X GET "https://api.example.com/endpoint" \
  -H "X-Api-Key: pk_comm_a1b2c3d4e5f6789..."
```

**JavaScript (fetch):**
```javascript
const response = await fetch('https://api.example.com/endpoint', {
  method: 'GET',
  headers: {
    'X-Api-Key': 'pk_comm_a1b2c3d4e5f6789...',
    'Content-Type': 'application/json'
  }
});
```

**JavaScript (axios):**
```javascript
const axios = require('axios');

const api = axios.create({
  baseURL: 'https://api.example.com',
  headers: {
    'X-Api-Key': 'pk_comm_a1b2c3d4e5f6789...'
  }
});

const response = await api.get('/endpoint');
```

**C# (HttpClient):**
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-Api-Key", "pk_comm_a1b2c3d4e5f6789...");

var response = await client.GetAsync("https://api.example.com/endpoint");
```

**Python (requests):**
```python
import requests

headers = {
    'X-Api-Key': 'pk_comm_a1b2c3d4e5f6789...'
}

response = requests.get('https://api.example.com/endpoint', headers=headers)
```

## Available Scopes

Your API key may be granted one or more of the following scopes:

| Scope | Description |
|-------|-------------|
| `auth:validate` | Validate JWT tokens |
| `auth:sync` | Synchronize authentication state, force-auth, external login |
| `users:read` | Read user information and profiles |
| `users:write` | Update user information (email, password, roles) |
| `assets:read` | Read/download assets |
| `assets:write` | Upload, register links, and delete assets |
| `sites:read` | Read site membership information |
| `push:send` | Send push notifications |
| `admin` | Full administrative access (grants all scopes) |

## Protected Endpoints by Scope

### `auth:validate`
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/validate` | Validate a JWT token |
| POST | `/auth/validate-token` | Validate token (cross-site) |

### `auth:sync`
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/force-auth` | Force authenticate as a user by ID |
| POST | `/auth/external-login` | Login via external provider (Google, Apple, etc.) |

### `users:read`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/profile` | Get current user's profile |
| GET | `/profile/{userId}` | Get profile by user ID |
| GET | `/profile/full` | Get full user info including sites |

### `users:write`
| Method | Endpoint | Description |
|--------|----------|-------------|
| PUT | `/profile` | Update current user's profile |
| PUT | `/admin/users/{id}` | Update user (email, password, etc.) |
| POST | `/sites/role` | Update user's site role |

### `assets:read`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/asset/{id}` | Get asset file by ID |
| GET | `/asset/{id}/info` | Get asset metadata |

### `assets:write`
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/asset/upload` | Upload an asset file |
| POST | `/asset/link` | Register an external link (YouTube, etc.) |
| DELETE | `/asset/{id}` | Delete an asset |

### `sites:read`
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/sites` | Get user's joined sites |
| POST | `/sites/join` | Join a site |
| POST | `/sites/leave` | Leave a site |
| GET | `/sites/check/{siteKey}` | Check site membership |

### `push:send`
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/push/user/{userId}` | Send notification to a user |
| POST | `/api/push/site/{siteKey}` | Send notification to site users |
| POST | `/api/push/broadcast` | Broadcast to all users |
| GET | `/api/push/user/{userId}/status` | Check if user is connected |
| POST | `/api/push/users/batch` | Send to multiple users |

**Push Notification Response Format:**

Push endpoints return detailed delivery status to help you track notification delivery:

**Single User (`POST /api/push/user/{userId}`):**
```json
{
  "success": true,
  "userOnline": true,
  "delivered": true,
  "error": null
}
```

**Site Notification (`POST /api/push/site/{siteKey}`):**
```json
{
  "success": true,
  "delivered": true,
  "connectedUsers": 15,
  "error": null
}
```

**Broadcast (`POST /api/push/broadcast`):**
```json
{
  "success": true,
  "delivered": true,
  "error": null
}
```

**Batch (`POST /api/push/users/batch`):**
```json
{
  "success": true,
  "totalUsers": 10,
  "onlineUsers": 7,
  "deliveredCount": 7,
  "results": [
    { "userId": 1, "userOnline": true, "delivered": true },
    { "userId": 2, "userOnline": false, "delivered": false }
  ]
}
```

**Response Fields:**
- `success`: Whether the operation completed without errors
- `userOnline`: Whether the user was connected via SignalR at the time
- `delivered`: Whether SignalR successfully delivered the notification
- `connectedUsers`: Number of users connected to the site (for site notifications)
- `error`: Error message if success is false

**Note:** The shared notification service handles real-time delivery only. Each site should persist notification history in its own database for user notification pages.

## Error Responses

### 401 Unauthorized

**Missing API Key (when JWT also not provided):**
```json
{
  "message": "Authentication required. Provide X-Api-Key header or valid JWT Bearer token."
}
```

**Invalid API Key:**
```json
{
  "message": "Invalid or expired API key."
}
```

### 403 Forbidden

**Insufficient Scope:**
```json
{
  "message": "This API key does not have the required scope: assets:write"
}
```

**IP Not Allowed:**
```json
{
  "message": "Access denied from this IP address."
}
```

## Best Practices

1. **Store securely**: Use environment variables or a secrets manager for your API key
2. **Never expose**: Do not include API keys in client-side code, URLs, or version control
3. **Rotate regularly**: Request a new key periodically and update your applications
4. **Use minimal scopes**: Only request the scopes your application needs
5. **Monitor usage**: Check with your administrator if you notice unexpected behavior

## Environment Variable Example

Store the key in an environment variable:

```bash
# .env file (do not commit to git)
FUNTIME_API_KEY=pk_comm_a1b2c3d4e5f6789...
```

Then use it in your code:

```javascript
// Node.js
const apiKey = process.env.FUNTIME_API_KEY;
```

```csharp
// C#
var apiKey = Environment.GetEnvironmentVariable("FUNTIME_API_KEY");
```

```python
# Python
import os
api_key = os.environ.get('FUNTIME_API_KEY')
```

## Support

If you encounter issues with your API key:
- Verify the key is copied correctly (no extra spaces)
- Check that your IP address is allowed (if IP restrictions are enabled)
- Confirm your key has the required scope for the endpoint
- Contact your system administrator if problems persist
