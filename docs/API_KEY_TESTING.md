# API Key Integration Testing Guide

This document explains how to test your API key integration with the Funtime Identity Service.

## Quick Start

Before integrating API calls into your application, verify your API key is working correctly using these test endpoints.

## Test Endpoints

| Endpoint | Auth Required | Description |
|----------|---------------|-------------|
| `GET /apikey/test` | Yes (API Key) | Validate your API key and see its configuration |
| `GET /apikey/test/scope/{scope}` | Yes (API Key) | Check if your key has a specific scope |
| `GET /apikey/scopes` | No | List all available scopes |

---

## 1. Test Your API Key

Validates your API key and returns its full configuration.

**Request:**
```bash
curl -X GET "https://identity.example.com/apikey/test" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "API key is valid and working!",
  "partnerKey": "mysite",
  "partnerName": "My Site Application",
  "scopes": ["auth:validate", "users:read", "push:send"],
  "rateLimitPerMinute": 60,
  "isActive": true,
  "clientIp": "192.168.1.100",
  "allowedIps": null,
  "expiresAt": null,
  "lastUsedAt": "2024-01-15T10:30:00Z",
  "serverTime": "2024-01-15T10:35:00Z"
}
```

**Error Response (401):**
```json
{
  "message": "Invalid API key."
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Always `true` for valid keys |
| `message` | string | Human-readable status message |
| `partnerKey` | string | Your unique partner identifier |
| `partnerName` | string | Your application's display name |
| `scopes` | string[] | List of permissions granted to your key |
| `rateLimitPerMinute` | number | Maximum requests allowed per minute |
| `isActive` | boolean | Whether the key is currently active |
| `clientIp` | string | Your request's IP address (useful for debugging IP restrictions) |
| `allowedIps` | string[] | IP whitelist (null = all IPs allowed) |
| `expiresAt` | string | Key expiration date (null = never expires) |
| `lastUsedAt` | string | Last time this key was used |
| `serverTime` | string | Current server time (UTC) |

---

## 2. Test a Specific Scope

Check if your API key has permission for a specific operation.

**Request:**
```bash
curl -X GET "https://identity.example.com/apikey/test/scope/push:send" \
  -H "X-Api-Key: YOUR_API_KEY_HERE"
```

**Success Response - Has Scope (200):**
```json
{
  "scope": "push:send",
  "isValidScope": true,
  "hasScope": true,
  "message": "Your API key has the 'push:send' scope."
}
```

**Success Response - Missing Scope (200):**
```json
{
  "scope": "admin",
  "isValidScope": true,
  "hasScope": false,
  "message": "Your API key does NOT have the 'admin' scope. Your scopes: auth:validate, users:read, push:send"
}
```

**Invalid Scope (200):**
```json
{
  "scope": "invalid:scope",
  "isValidScope": false,
  "hasScope": false,
  "message": "'invalid:scope' is not a valid scope. Valid scopes: auth:validate, auth:sync, users:read, users:write, assets:read, assets:write, sites:read, push:send, admin"
}
```

---

## 3. List Available Scopes

Get a list of all available scopes (no authentication required).

**Request:**
```bash
curl -X GET "https://identity.example.com/apikey/scopes"
```

**Response (200):**
```json
{
  "scopes": [
    { "name": "auth:validate", "description": "Validate JWT tokens" },
    { "name": "auth:sync", "description": "Sync user authentication (force-auth, external login)" },
    { "name": "users:read", "description": "Read user profiles and information" },
    { "name": "users:write", "description": "Update user information (email, password, roles)" },
    { "name": "assets:read", "description": "Read/download assets" },
    { "name": "assets:write", "description": "Upload, link, and delete assets" },
    { "name": "sites:read", "description": "Read site membership information" },
    { "name": "push:send", "description": "Send push notifications via SignalR" },
    { "name": "admin", "description": "Full administrative access (includes all scopes)" }
  ]
}
```

---

## Integration Examples

### JavaScript/TypeScript

```typescript
const API_KEY = process.env.FUNTIME_API_KEY;
const BASE_URL = 'https://identity.example.com';

async function testApiKey(): Promise<boolean> {
  try {
    const response = await fetch(`${BASE_URL}/apikey/test`, {
      headers: { 'X-Api-Key': API_KEY }
    });

    if (!response.ok) {
      console.error('API key test failed:', response.status);
      return false;
    }

    const result = await response.json();
    console.log('API Key Valid!');
    console.log('Partner:', result.partnerName);
    console.log('Scopes:', result.scopes.join(', '));
    console.log('Rate Limit:', result.rateLimitPerMinute, 'req/min');
    return true;
  } catch (error) {
    console.error('API key test error:', error);
    return false;
  }
}

async function hasScope(scope: string): Promise<boolean> {
  const response = await fetch(`${BASE_URL}/apikey/test/scope/${scope}`, {
    headers: { 'X-Api-Key': API_KEY }
  });
  const result = await response.json();
  return result.hasScope;
}

// Usage
await testApiKey();
if (await hasScope('push:send')) {
  console.log('Can send push notifications!');
}
```

### C# / .NET

```csharp
public class ApiKeyTester
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ApiKeyTester(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task<ApiKeyTestResult?> TestApiKeyAsync()
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/apikey/test");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"API key test failed: {response.StatusCode}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiKeyTestResult>(json);
    }

    public async Task<bool> HasScopeAsync(string scope)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/apikey/test/scope/{scope}");
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ScopeTestResult>(json);
        return result?.HasScope ?? false;
    }
}

// Usage
var tester = new ApiKeyTester("https://identity.example.com", apiKey);
var result = await tester.TestApiKeyAsync();
if (result != null)
{
    Console.WriteLine($"Partner: {result.PartnerName}");
    Console.WriteLine($"Scopes: {string.Join(", ", result.Scopes)}");
}
```

### Python

```python
import requests
import os

API_KEY = os.environ.get('FUNTIME_API_KEY')
BASE_URL = 'https://identity.example.com'

def test_api_key():
    response = requests.get(
        f'{BASE_URL}/apikey/test',
        headers={'X-Api-Key': API_KEY}
    )

    if response.status_code != 200:
        print(f'API key test failed: {response.status_code}')
        return None

    result = response.json()
    print(f"API Key Valid!")
    print(f"Partner: {result['partnerName']}")
    print(f"Scopes: {', '.join(result['scopes'])}")
    print(f"Rate Limit: {result['rateLimitPerMinute']} req/min")
    return result

def has_scope(scope):
    response = requests.get(
        f'{BASE_URL}/apikey/test/scope/{scope}',
        headers={'X-Api-Key': API_KEY}
    )
    result = response.json()
    return result.get('hasScope', False)

# Usage
if test_api_key():
    if has_scope('push:send'):
        print('Can send push notifications!')
```

---

## Troubleshooting

### Common Issues

| Issue | Possible Cause | Solution |
|-------|----------------|----------|
| 401 Unauthorized | Invalid or expired API key | Verify your key is correct and not expired |
| 401 Unauthorized | Missing X-Api-Key header | Ensure header name is exactly `X-Api-Key` |
| 403 Forbidden | IP not in whitelist | Check `clientIp` in test response, contact admin to whitelist |
| 403 Forbidden | Missing required scope | Check `scopes` in test response, request additional scopes |
| 429 Too Many Requests | Rate limit exceeded | Check `rateLimitPerMinute`, reduce request frequency |

### Debugging Checklist

1. **Test your key first** - Always call `/apikey/test` before debugging other endpoints
2. **Check your IP** - The `clientIp` field shows what IP the server sees
3. **Verify scopes** - Use `/apikey/test/scope/{scope}` to confirm permissions
4. **Check expiration** - The `expiresAt` field shows if your key will expire
5. **Monitor rate limits** - The `rateLimitPerMinute` field shows your limit

### Health Check Integration

Add API key validation to your application startup:

```typescript
// startup.ts
async function validateApiKeyOnStartup() {
  console.log('Validating API key...');

  const response = await fetch(`${BASE_URL}/apikey/test`, {
    headers: { 'X-Api-Key': process.env.API_KEY }
  });

  if (!response.ok) {
    throw new Error('Invalid API key - application cannot start');
  }

  const result = await response.json();

  // Verify required scopes
  const requiredScopes = ['auth:validate', 'push:send'];
  const missingScopes = requiredScopes.filter(s => !result.scopes.includes(s));

  if (missingScopes.length > 0) {
    throw new Error(`Missing required scopes: ${missingScopes.join(', ')}`);
  }

  console.log(`API key valid for ${result.partnerName}`);
  console.log(`Scopes: ${result.scopes.join(', ')}`);
}
```

---

## Next Steps

Once your API key is validated:

1. **Read the full API documentation** - See [API_KEY_USAGE.md](./API_KEY_USAGE.md)
2. **Integrate authentication** - Use `auth:validate` scope to validate user tokens
3. **Set up notifications** - Use `push:send` scope to send real-time notifications
4. **Connect SignalR** - See SignalR Client Integration section in API_KEY_USAGE.md
