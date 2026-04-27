using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WeatherAPI.DTOs;
using WeatherAPI.Services;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly JwtOptions _jwtOptions;
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IOptions<JwtOptions> jwtOptions,
        AppDbContext context,
        IEmailSender emailSender)
    {
        _userManager  = userManager;
        _signInManager = signInManager;
        _jwtOptions   = jwtOptions.Value;
        _context      = context;
        _emailSender  = emailSender;
    }

    [HttpPost("register-customer")]
    public async Task<IActionResult> RegisterCustomer([FromBody] RegisterDto model)
        => await RegisterByRole(model, "Customer");

    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterDto model)
        => await RegisterByRole(model, "Admin");

    [HttpPost("register-vendor")]
    public async Task<IActionResult> RegisterVendor([FromBody] RegisterDto model)
        => await RegisterByRole(model, "Vendor");

    // ── Registration (with DB transaction + email confirmation) ───────────
    // Both CreateAsync and AddToRoleAsync run inside a single DB transaction.
    // If role assignment fails the user row is rolled back automatically.
    // After success an email confirmation link is sent to the user.
    private async Task<IActionResult> RegisterByRole(RegisterDto model, string role)
    {
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
            return Conflict(new { message = "User with this email already exists." });

        var user = new IdentityUser { UserName = model.Email, Email = model.Email };

        // Wrap CreateAsync + AddToRoleAsync in a single DB transaction so that
        // a failed role assignment never leaves a half-created user in the DB.
        await using var tx = await _context.Database.BeginTransactionAsync();

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            await tx.RollbackAsync();
            return BadRequest(new {
                message = "Registration failed.",
                errors  = createResult.Errors.Select(e => e.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await tx.RollbackAsync();
            return BadRequest(new {
                message = "Role assignment failed.",
                errors  = roleResult.Errors.Select(e => e.Description)
            });
        }

        await tx.CommitAsync();

        // Generate email confirmation token and send the link to the user.
        await SendConfirmationEmailAsync(user);

        return Ok(new {
            message = $"{role} registered successfully. Please check your email to confirm your account.",
            userId  = user.Id,
            email   = user.Email,
            role
        });
    }

    // ── Email Confirmation ─────────────────────────────────────────────────
    // GET /api/auth/confirm-email?userId=...&token=...
    // The link sent to the user hits this endpoint to mark the email as confirmed.
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Invalid email confirmation link." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Token comes URL-encoded from the email link; decode it first.
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
            return BadRequest(new {
                message = "Email confirmation failed.",
                errors  = result.Errors.Select(e => e.Description)
            });

        return Ok(new { message = "Email confirmed successfully. You can now log in." });
    }

    // ── Login ──────────────────────────────────────────────────────────────
    // Blocks login if the user has not confirmed their email address yet.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized("Invalid login attempt.");

        // Reject login for users whose email address has not been confirmed.
        if (!user.EmailConfirmed)
            return Unauthorized(new { message = "Please confirm your email address before logging in." });

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, model.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized("Invalid credentials.");

        var roles         = await _userManager.GetRolesAsync(user);
        var expiryDateUtc = DateTime.UtcNow.AddHours(_jwtOptions.ExpiryHours);
        var token         = GenerateJwtToken(user, roles, expiryDateUtc);

        return Ok(new {
            token,
            tokenType    = "Bearer",
            expiresAtUtc = expiryDateUtc,
            user = new { id = user.Id, email = user.Email, roles }
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email  = User.FindFirstValue(ClaimTypes.Email),
            roles
        });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task SendConfirmationEmailAsync(IdentityUser user)
    {
        var rawToken    = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        // Build the confirmation URL pointing back to the API endpoint.
        var confirmUrl = $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email"
                       + $"?userId={Uri.EscapeDataString(user.Id)}"
                       + $"&token={Uri.EscapeDataString(encodedToken)}";

        var subject = "Confirm your email address";
        var body    = $@"
            <h2>Welcome!</h2>
            <p>Please confirm your email address by clicking the link below:</p>
            <p><a href=""{confirmUrl}"">Confirm Email</a></p>
            <p>If you did not create an account, you can ignore this email.</p>";

        await _emailSender.SendEmailAsync(user.Email!, subject, body);
    }

    private string GenerateJwtToken(
        IdentityUser user, IEnumerable<string> roles, DateTime expiryDateUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email,              user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier,     user.Id),
            new(ClaimTypes.Name,               user.UserName ?? user.Email ?? user.Id)
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwtOptions.Issuer,
            audience:           _jwtOptions.Audience,
            claims:             claims,
            expires:            expiryDateUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
