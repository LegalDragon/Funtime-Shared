# API Key Authentication Guide

This document describes how to authenticate API requests using API keys for trusted partner applications.

## Overview

API keys provide a simple way for trusted applications to authenticate with the Funtime Identity API without user credentials. Each key is scoped to specific permissions and can be restricted by IP address.

## Getting an API Key

Contact your system administrator to obtain an API key. You will receive:
- **API Key**: A 64-character string (e.g., `fk_a1b2c3d4e5f6...`)
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
  -H "X-Api-Key: fk_a1b2c3d4e5f6789..."
```

**JavaScript (fetch):**
```javascript
const response = await fetch('https://api.example.com/endpoint', {
  method: 'GET',
  headers: {
    'X-Api-Key': 'fk_a1b2c3d4e5f6789...',
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
    'X-Api-Key': 'fk_a1b2c3d4e5f6789...'
  }
});

const response = await api.get('/endpoint');
```

**C# (HttpClient):**
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-Api-Key", "fk_a1b2c3d4e5f6789...");

var response = await client.GetAsync("https://api.example.com/endpoint");
```

**Python (requests):**
```python
import requests

headers = {
    'X-Api-Key': 'fk_a1b2c3d4e5f6789...'
}

response = requests.get('https://api.example.com/endpoint', headers=headers)
```

## Available Scopes

Your API key may be granted one or more of the following scopes:

| Scope | Description |
|-------|-------------|
| `auth:validate` | Validate authentication tokens |
| `auth:sync` | Synchronize authentication state between services |
| `users:read` | Read user information |
| `users:write` | Create and update user information |
| `assets:read` | Read/download assets |
| `assets:write` | Upload and manage assets |
| `push:send` | Send push notifications |
| `admin` | Full administrative access |

## Error Responses

### 401 Unauthorized

**Missing API Key:**
```json
{
  "error": "API key is required"
}
```

**Invalid API Key:**
```json
{
  "error": "Invalid API key"
}
```

**Inactive API Key:**
```json
{
  "error": "API key is inactive"
}
```

**Expired API Key:**
```json
{
  "error": "API key has expired"
}
```

### 403 Forbidden

**Insufficient Scope:**
```json
{
  "error": "API key does not have required scope: assets:write"
}
```

**IP Not Allowed:**
```json
{
  "error": "IP address not allowed for this API key"
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
FUNTIME_API_KEY=fk_a1b2c3d4e5f6789...
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
