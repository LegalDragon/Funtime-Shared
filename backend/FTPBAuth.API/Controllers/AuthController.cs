using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FTPBAuth.API.Data;
using FTPBAuth.API.DTOs;
using FTPBAuth.API.Models;
using FTPBAuth.API.Services;

namespace FTPBAuth.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IOtpService _otpService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        IJwtService jwtService,
        IOtpService otpService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _otpService = otpService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user with email and password
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (existingUser != null)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Email is already registered."
            });
        }

        // Create new user
        var user = new User
        {
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate token
        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = "Registration successful.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password."
            });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password."
            });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = "Login successful.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Send OTP to phone number
    /// </summary>
    [HttpPost("otp/send")]
    public async Task<ActionResult<ApiResponse>> SendOtp([FromBody] OtpSendRequest request)
    {
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

        var (success, message) = await _otpService.SendOtpAsync(normalizedPhone);

        if (!success)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ApiResponse
        {
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Verify OTP and login/register with phone number
    /// </summary>
    [HttpPost("otp/verify")]
    public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] OtpVerifyRequest request)
    {
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

        var (success, message) = await _otpService.VerifyOtpAsync(normalizedPhone, request.Code);

        if (!success)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = message
            });
        }

        // Find or create user by phone number
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);

        if (user == null)
        {
            // Create new user with phone number only
            user = new User
            {
                PhoneNumber = normalizedPhone,
                IsPhoneVerified = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
        }
        else
        {
            user.IsPhoneVerified = true;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("User logged in via OTP: {PhoneNumber}", normalizedPhone);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = "OTP verification successful.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Link a phone number to an existing email account
    /// Requires OTP verification first (call otp/send before this)
    /// </summary>
    [Authorize]
    [HttpPost("link-phone")]
    public async Task<ActionResult<AuthResponse>> LinkPhone([FromBody] LinkPhoneRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid token."
            });
        }

        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);

        // Check if phone number is already linked to another account
        var existingUserWithPhone = await _context.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone && u.Id != userId);

        if (existingUserWithPhone != null)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "This phone number is already linked to another account."
            });
        }

        // Verify OTP
        var (success, message) = await _otpService.VerifyOtpAsync(normalizedPhone, request.Code);

        if (!success)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = message
            });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "User not found."
            });
        }

        user.PhoneNumber = normalizedPhone;
        user.IsPhoneVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("Phone linked to user {UserId}: {PhoneNumber}", userId, normalizedPhone);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = "Phone number linked successfully.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Link an email/password to an existing phone-only account
    /// </summary>
    [Authorize]
    [HttpPost("link-email")]
    public async Task<ActionResult<AuthResponse>> LinkEmail([FromBody] LinkEmailRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid token."
            });
        }

        var normalizedEmail = request.Email.ToLower();

        // Check if email is already linked to another account
        var existingUserWithEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Id != userId);

        if (existingUserWithEmail != null)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "This email is already linked to another account."
            });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "User not found."
            });
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "This account already has an email address."
            });
        }

        user.Email = normalizedEmail;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("Email linked to user {UserId}: {Email}", userId, normalizedEmail);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = "Email linked successfully.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse
            {
                Success = false,
                Message = "Invalid token."
            });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                Message = "User not found."
            });
        }

        return Ok(MapToUserResponse(user));
    }

    /// <summary>
    /// Validate a JWT token
    /// </summary>
    [HttpPost("validate")]
    public ActionResult<ValidateTokenResponse> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var (isValid, userId, email, phoneNumber) = _jwtService.ValidateToken(request.Token);

        if (!isValid)
        {
            return Ok(new ValidateTokenResponse
            {
                Valid = false,
                Message = "Invalid or expired token."
            });
        }

        return Ok(new ValidateTokenResponse
        {
            Valid = true,
            UserId = userId,
            Email = email,
            PhoneNumber = phoneNumber,
            Message = "Token is valid."
        });
    }

    /// <summary>
    /// Force authenticate as a user by ID (for legacy system integration)
    /// Requires API secret key
    /// </summary>
    [HttpPost("force-auth")]
    public async Task<ActionResult<AuthResponse>> ForceAuth([FromBody] ForceAuthRequest request)
    {
        var configuredSecret = _configuration["ApiSecretKey"];

        if (string.IsNullOrEmpty(configuredSecret) || configuredSecret == "YOUR_SUPER_SECRET_API_KEY_CHANGE_IN_PRODUCTION")
        {
            _logger.LogError("Force auth attempted but API secret key is not configured");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "API secret key is not configured."
            });
        }

        if (request.ApiSecretKey != configuredSecret)
        {
            _logger.LogWarning("Force auth attempted with invalid API secret key for user {UserId}", request.UserId);
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid API secret key."
            });
        }

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "User not found."
            });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("Force auth successful for user {UserId}", request.UserId);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = "Force authentication successful.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Login or register via external provider (Google, Apple, WeChat, etc.)
    /// Requires API secret key for server-to-server authentication
    /// </summary>
    [HttpPost("external-login")]
    public async Task<ActionResult<AuthResponse>> ExternalLogin([FromBody] ExternalLoginRequest request)
    {
        var configuredSecret = _configuration["ApiSecretKey"];

        if (string.IsNullOrEmpty(configuredSecret) || configuredSecret == "YOUR_SUPER_SECRET_API_KEY_CHANGE_IN_PRODUCTION")
        {
            _logger.LogError("External login attempted but API secret key is not configured");
            return StatusCode(500, new AuthResponse
            {
                Success = false,
                Message = "API secret key is not configured."
            });
        }

        if (request.ApiSecretKey != configuredSecret)
        {
            _logger.LogWarning("External login attempted with invalid API secret key for provider {Provider}", request.Provider);
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid API secret key."
            });
        }

        var normalizedProvider = request.Provider.ToLower();

        // Check if this external login already exists
        var existingLogin = await _context.ExternalLogins
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == normalizedProvider && e.ProviderUserId == request.ProviderUserId);

        User user;

        if (existingLogin != null)
        {
            // Existing user - update last used and login
            user = existingLogin.User;
            existingLogin.LastUsedAt = DateTime.UtcNow;
            existingLogin.ProviderEmail = request.ProviderEmail;
            existingLogin.ProviderDisplayName = request.ProviderDisplayName;
        }
        else
        {
            // New user - create user and external login
            user = new User
            {
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Save to get the user ID

            var externalLogin = new ExternalLogin
            {
                UserId = user.Id,
                Provider = normalizedProvider,
                ProviderUserId = request.ProviderUserId,
                ProviderEmail = request.ProviderEmail,
                ProviderDisplayName = request.ProviderDisplayName,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };
            _context.ExternalLogins.Add(externalLogin);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("User logged in via {Provider}: {ProviderUserId}", normalizedProvider, request.ProviderUserId);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = "External login successful.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Link an external provider to the current user's account
    /// </summary>
    [Authorize]
    [HttpPost("link-external")]
    public async Task<ActionResult<AuthResponse>> LinkExternalLogin([FromBody] LinkExternalLoginRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid token."
            });
        }

        var normalizedProvider = request.Provider.ToLower();

        // Check if this provider is already linked to another user
        var existingLogin = await _context.ExternalLogins
            .FirstOrDefaultAsync(e => e.Provider == normalizedProvider && e.ProviderUserId == request.ProviderUserId);

        if (existingLogin != null)
        {
            if (existingLogin.UserId == userId)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "This external login is already linked to your account."
                });
            }

            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "This external login is already linked to another account."
            });
        }

        // Check if user already has this provider linked
        var userExistingProvider = await _context.ExternalLogins
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == normalizedProvider);

        if (userExistingProvider != null)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = $"You already have a {request.Provider} account linked."
            });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "User not found."
            });
        }

        var externalLogin = new ExternalLogin
        {
            UserId = user.Id,
            Provider = normalizedProvider,
            ProviderUserId = request.ProviderUserId,
            ProviderEmail = request.ProviderEmail,
            ProviderDisplayName = request.ProviderDisplayName,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExternalLogins.Add(externalLogin);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("External login {Provider} linked to user {UserId}", normalizedProvider, userId);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = $"{request.Provider} account linked successfully.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Unlink an external provider from the current user's account
    /// </summary>
    [Authorize]
    [HttpPost("unlink-external")]
    public async Task<ActionResult<AuthResponse>> UnlinkExternalLogin([FromBody] UnlinkExternalLoginRequest request)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                Message = "Invalid token."
            });
        }

        var normalizedProvider = request.Provider.ToLower();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = "User not found."
            });
        }

        // Count how many login methods the user has
        var externalLoginCount = await _context.ExternalLogins.CountAsync(e => e.UserId == userId);
        var hasEmail = !string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(user.PasswordHash);
        var hasPhone = !string.IsNullOrEmpty(user.PhoneNumber);
        var totalLoginMethods = externalLoginCount + (hasEmail ? 1 : 0) + (hasPhone ? 1 : 0);

        if (totalLoginMethods <= 1)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Cannot unlink the only login method. Please link another login method first."
            });
        }

        var externalLogin = await _context.ExternalLogins
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == normalizedProvider);

        if (externalLogin == null)
        {
            return NotFound(new AuthResponse
            {
                Success = false,
                Message = $"No {request.Provider} account is linked to your account."
            });
        }

        _context.ExternalLogins.Remove(externalLogin);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("External login {Provider} unlinked from user {UserId}", normalizedProvider, userId);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = token,
            Message = $"{request.Provider} account unlinked successfully.",
            User = MapToUserResponse(user)
        });
    }

    /// <summary>
    /// Get current user's linked external logins
    /// </summary>
    [Authorize]
    [HttpGet("external-logins")]
    public async Task<ActionResult<List<ExternalLoginResponse>>> GetExternalLogins()
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
        {
            return Unauthorized(new ApiResponse
            {
                Success = false,
                Message = "Invalid token."
            });
        }

        var externalLogins = await _context.ExternalLogins
            .Where(e => e.UserId == userId)
            .Select(e => new ExternalLoginResponse
            {
                Provider = e.Provider,
                ProviderUserId = e.ProviderUserId,
                ProviderEmail = e.ProviderEmail,
                ProviderDisplayName = e.ProviderDisplayName,
                CreatedAt = e.CreatedAt,
                LastUsedAt = e.LastUsedAt
            })
            .ToListAsync();

        return Ok(externalLogins);
    }

    private int? GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // Remove any non-digit characters except +
        var normalized = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Ensure it starts with +
        if (!normalized.StartsWith('+'))
        {
            normalized = '+' + normalized;
        }

        return normalized;
    }

    private static UserResponse MapToUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
