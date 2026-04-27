# API Changes Documentation

## Overview

| Feature | Files Changed |
|---|---|
| Product Filtering | `DTOs/ProductDtos.cs`, `Controllers/ProductsController.cs` |
| In-Memory Caching (5 min) | `Program.cs`, `Controllers/ProductsController.cs` |
| Transaction on Registration | `Controllers/AuthController.cs` |
| Product Image Upload | `models/Product.cs`, `DTOs/ProductDtos.cs`, `Controllers/ProductsController.cs`, `Program.cs` |
| Email Confirmation | `Services/EmailSender.cs` *(new)*, `Controllers/AuthController.cs`, `Program.cs`, `appsettings.json` |

---

## File: `models/Product.cs`

**Change:** Added optional `ImageUrl` field to store the relative URL of the uploaded product image.

```csharp
public class Product
{
   public int Id { get; set; }
   public string Name { get; set; }
   public string SKU { get; set; }
   public decimal Price { get; set; }
   public int Stock { get; set; }

   // ✅ NEW — stores the relative URL of the uploaded image e.g. /uploads/products/abc.jpg
   public string? ImageUrl { get; set; }

   // Many Products belong to one Category (M-to-1)
   public int CategoryId { get; set; }
   public Category Category { get; set; }

   // Many Products belong to one Supplier (M-to-1)
   public int SupplierId { get; set; }
   public Supplier Supplier { get; set; }

   // One Product can appear in many OrderItems (1-to-M)
   public ICollection<OrderItem> OrderItems { get; set; }
}
```

---

## File: `DTOs/ProductDtos.cs`

**Changes:**
- Added `ProductFilterDto` — query parameters for filtering the product list
- Added `IFormFile? Image` to `CreateProductDto` and `UpdateProductDto` — enables file upload in Swagger
- Added `string? ImageUrl` to `ProductDto` — returned in responses

```csharp
using System.ComponentModel.DataAnnotations;

namespace WeatherAPI.DTOs;

// ✅ NEW — all fields are optional; pass any combination as query params
// e.g. GET /api/products?name=shirt&minPrice=10&categoryId=2
public class ProductFilterDto
{
    public string? Name { get; set; }       // partial, case-insensitive match
    public string? SKU { get; set; }        // exact, case-insensitive match
    public decimal? MinPrice { get; set; }  // inclusive lower bound
    public decimal? MaxPrice { get; set; }  // inclusive upper bound
    public int? CategoryId { get; set; }    // filter by category
}

public class CreateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int SupplierId { get; set; }

    // ✅ NEW — optional image file; IFormFile makes Swagger show a file picker
    public IFormFile? Image { get; set; }
}

public class UpdateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int SupplierId { get; set; }

    // ✅ NEW — provide a new file to replace the image; leave empty to keep existing
    public IFormFile? Image { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public int SupplierId { get; set; }

    // ✅ NEW — relative URL returned in responses e.g. /uploads/products/abc.jpg
    public string? ImageUrl { get; set; }
}

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class BulkPriceUpdateDto
{
    public int ProductId { get; set; }
    public decimal NewPrice { get; set; }
}
```

---

## File: `Controllers/ProductsController.cs`

**Changes:**
- Injected `IMemoryCache` and `IWebHostEnvironment`
- `GetAll` — accepts `ProductFilterDto`, applies dynamic filters, caches unfiltered results for 5 min
- `Create` — switched to `[FromForm]` + `[Consumes("multipart/form-data")]`, saves image via `SaveImageAsync`
- `Update` — same as Create; only updates image when a new file is provided
- Added private helper `SaveImageAsync` — validates and saves the uploaded file
- Added private helper `MapToDto` — maps Product entity to ProductDto

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WeatherAPI.DTOs;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _env; // ✅ NEW — needed to resolve wwwroot path

    // ✅ NEW — single fixed key for the full unfiltered product list
    private const string AllProductsCacheKey = "products_all";

    public ProductsController(AppDbContext context, IMemoryCache cache, IWebHostEnvironment env)
    {
        _context = context;
        _cache   = cache;
        _env     = env; // ✅ NEW
    }

    // ✅ CHANGED — now accepts optional filter params and applies in-memory caching
    // GET /api/products
    // GET /api/products?name=shirt&minPrice=10&maxPrice=100&categoryId=2
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter)
    {
        // Check if any filter was provided
        bool hasFilter = !string.IsNullOrWhiteSpace(filter.Name)
                      || !string.IsNullOrWhiteSpace(filter.SKU)
                      || filter.MinPrice.HasValue
                      || filter.MaxPrice.HasValue
                      || filter.CategoryId.HasValue;

        // ✅ Cache hit — return immediately without hitting the DB (only for unfiltered requests)
        if (!hasFilter && _cache.TryGetValue(AllProductsCacheKey, out List<ProductDto>? cached))
            return Ok(cached);

        var query = _context.Products.AsQueryable();

        // ✅ Each filter is only applied when actually provided
        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(p => p.Name.ToLower().Contains(filter.Name.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.SKU))
            query = query.Where(p => p.SKU.ToLower() == filter.SKU.ToLower());

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        var results = await query
            .Select(p => new ProductDto
            {
                Id         = p.Id,
                Name       = p.Name,
                SKU        = p.SKU,
                Price      = p.Price,
                Stock      = p.Stock,
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId,
                ImageUrl   = p.ImageUrl // ✅ NEW field
            })
            .ToListAsync();

        // ✅ Store in cache for 5 minutes — only when no filters are applied
        if (!hasFilter)
            _cache.Set(AllProductsCacheKey, results, TimeSpan.FromMinutes(5));

        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return Ok(MapToDto(product));
    }

    [HttpGet("{id:int}/supplier")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        var product = await _context.Products.Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        var s = product.Supplier;
        return Ok(new SupplierDto { Id = s.Id, Name = s.Name, Email = s.Email, Phone = s.Phone });
    }

    [HttpGet("{id:int}/category")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        var c = product.Category;
        return Ok(new CategoryDto { Id = c.Id, Name = c.Name });
    }

    // ✅ CHANGED — [Consumes("multipart/form-data")] + [FromForm] makes Swagger show file picker
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateProductDto dto)
    {
        var product = new Product
        {
            Name       = dto.Name,
            SKU        = dto.SKU,
            Price      = dto.Price,
            Stock      = dto.Stock,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId,
            ImageUrl   = await SaveImageAsync(dto.Image) // ✅ NEW — save file, store URL
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, MapToDto(product));
    }

    // ✅ CHANGED — same multipart change; only updates image when a new file is provided
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.Name       = dto.Name;
        product.SKU        = dto.SKU;
        product.Price      = dto.Price;
        product.Stock      = dto.Stock;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;

        // ✅ NEW — only replace the image when a new file is uploaded
        if (dto.Image != null)
            product.ImageUrl = await SaveImageAsync(dto.Image);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkInsert(List<CreateProductDto> dtos)
    {
        var products = dtos.Select(dto => new Product
        {
            Name       = dto.Name,
            SKU        = dto.SKU,
            Price      = dto.Price,
            Stock      = dto.Stock,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId
        }).ToList();
        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();
        return Ok(new { inserted = products.Count });
    }

    [HttpGet("with-details")]
    public async Task<IActionResult> WithDetails()
    {
        var data = await _context.Products
            .Select(p => new
            {
                p.Id, p.Name, p.SKU, p.Price, p.Stock, p.ImageUrl,
                Category = new CategoryDto { Id = p.Category.Id, Name = p.Category.Name },
                Supplier = new SupplierDto { Id = p.Supplier.Id, Name = p.Supplier.Name, Email = p.Supplier.Email, Phone = p.Supplier.Phone }
            })
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count()
        => Ok(new { totalProducts = await _context.Products.CountAsync() });

    [HttpGet("high-price")]
    public async Task<IActionResult> HighPrice([FromQuery] decimal minPrice = 100)
    {
        var data = await _context.Products
            .Where(p => p.Price > minPrice)
            .Select(p => new ProductDto
            {
                Id = p.Id, Name = p.Name, SKU = p.SKU, Price = p.Price,
                Stock = p.Stock, CategoryId = p.CategoryId, SupplierId = p.SupplierId, ImageUrl = p.ImageUrl
            })
            .ToListAsync();
        return Ok(data);
    }

    [HttpPut("bulk-update-price")]
    public async Task<IActionResult> BulkUpdatePrice(List<BulkPriceUpdateDto> updates)
    {
        var ids      = updates.Select(x => x.ProductId).ToList();
        var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        foreach (var p in products)
        {
            var u = updates.First(x => x.ProductId == p.Id);
            p.Price = u.NewPrice;
        }
        await _context.SaveChangesAsync();
        return Ok(new { updated = products.Count });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    // ✅ NEW — validates and saves an uploaded image to wwwroot/uploads/products/
    // Returns the relative URL string, or null if no file was provided.
    private async Task<string?> SaveImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new InvalidOperationException("Unsupported image type. Allowed: jpg, jpeg, png, gif, webp.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Image size exceeds 5 MB limit.");

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadDir); // creates folder if it doesn't exist

        var fileName = $"{Guid.NewGuid()}{ext}"; // random name prevents collisions
        await using var stream = System.IO.File.Create(Path.Combine(uploadDir, fileName));
        await file.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    // ✅ NEW — single place to map Product entity → ProductDto
    private static ProductDto MapToDto(Product p) => new()
    {
        Id         = p.Id,
        Name       = p.Name,
        SKU        = p.SKU,
        Price      = p.Price,
        Stock      = p.Stock,
        CategoryId = p.CategoryId,
        SupplierId = p.SupplierId,
        ImageUrl   = p.ImageUrl
    };
}
```

---

## File: `Controllers/AuthController.cs`

**Changes:**
- Injected `AppDbContext` and `IEmailSender`
- `RegisterByRole` — wrapped in a DB transaction; sends confirmation email after commit
- Added `ConfirmEmail` endpoint — validates the token and marks email as confirmed
- `Login` — added `EmailConfirmed` guard before password check

```csharp
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
using WeatherAPI.Services; // ✅ NEW

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly JwtOptions _jwtOptions;
    private readonly AppDbContext _context;   // ✅ NEW — needed for DB transaction
    private readonly IEmailSender _emailSender; // ✅ NEW — sends confirmation email

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IOptions<JwtOptions> jwtOptions,
        AppDbContext context,       // ✅ NEW
        IEmailSender emailSender)   // ✅ NEW
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _jwtOptions    = jwtOptions.Value;
        _context       = context;
        _emailSender   = emailSender;
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

    // ✅ CHANGED — now uses a DB transaction + sends confirmation email
    private async Task<IActionResult> RegisterByRole(RegisterDto model, string role)
    {
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
            return Conflict(new { message = "User with this email already exists." });

        var user = new IdentityUser { UserName = model.Email, Email = model.Email };

        // ✅ NEW — begin transaction so CreateAsync + AddToRoleAsync are atomic.
        // If role assignment fails, the user row is also rolled back.
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
            await tx.RollbackAsync(); // rolls back the user creation too
            return BadRequest(new {
                message = "Role assignment failed.",
                errors  = roleResult.Errors.Select(e => e.Description)
            });
        }

        await tx.CommitAsync(); // ✅ both steps succeeded — save everything

        // ✅ NEW — generate token and email confirmation link to the user
        await SendConfirmationEmailAsync(user);

        return Ok(new {
            message = $"{role} registered successfully. Please check your email to confirm your account.",
            userId  = user.Id,
            email   = user.Email,
            role
        });
    }

    // ✅ NEW ENDPOINT — GET /api/auth/confirm-email?userId=...&token=...
    // User clicks this link from their email to confirm their address.
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return BadRequest(new { message = "Invalid email confirmation link." });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Decode the Base64Url-encoded token back to the raw token string
        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
            return BadRequest(new {
                message = "Email confirmation failed.",
                errors  = result.Errors.Select(e => e.Description)
            });

        return Ok(new { message = "Email confirmed successfully. You can now log in." });
    }

    // ✅ CHANGED — blocks login if email is not confirmed
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized("Invalid login attempt.");

        // ✅ NEW — reject login until email is confirmed
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

    // ✅ NEW — generates token, builds confirmation URL, sends HTML email
    private async Task SendConfirmationEmailAsync(IdentityUser user)
    {
        var rawToken     = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

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
```

---

## File: `Services/EmailSender.cs` *(new file)*

**Purpose:** SMTP email service used to send the confirmation email on registration.

```csharp
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WeatherAPI.Services;

// Settings bound from appsettings.json → "Email" section
public class EmailOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string From { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

// Interface — swap implementation later (e.g. SendGrid) without changing controllers
public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
}

// Concrete SMTP implementation
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> opts, ILogger<SmtpEmailSender> logger)
    {
        _opts   = opts.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
        {
            EnableSsl   = _opts.EnableSsl,
            Credentials = new NetworkCredential(_opts.From, _opts.Password)
        };

        var message = new MailMessage(_opts.From, to, subject, htmlBody)
        {
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // SMTP failure is logged but NOT thrown.
            // Registration succeeds even if the mail server is down.
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
```

---

## File: `Program.cs`

**Changes:** Added `AddMemoryCache`, `EmailOptions`, `SmtpEmailSender`, and `UseStaticFiles`.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WeatherAPI.Services; // ✅ NEW

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ NEW — in-memory cache used by ProductsController (5-minute product list cache)
builder.Services.AddMemoryCache();

// ✅ NEW — bind Email config section and register the SMTP email sender
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<ExternalServicesOptions>(builder.Configuration.GetSection("ExternalServices"));

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.AddOptions<JwtOptions>()
    .Bind(jwtSection)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtOptions.Issuer,
        ValidAudience            = jwtOptions.Audience,
        IssuerSigningKey         = signingKey,
        ClockSkew                = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAllOrigins");

// ✅ NEW — serve files from wwwroot/ so uploaded product images are accessible via URL
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## File: `appsettings.json`

**Change:** Added `"Email"` section for SMTP configuration.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=aspnet;Username=YOUR_USER;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Issuer": "WeatherApi Backend",
    "Audience": "WeatherApiClient",
    "Secret": "YOUR_JWT_SECRET",
    "ExpiryHours": 12
  },
  // ✅ NEW — SMTP settings for confirmation emails
  // For Gmail: generate an App Password at Google Account → Security → App passwords
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "From": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true
  }
}
```

---

## Database Migration

A new EF migration was generated and applied to add the `ImageUrl` column:

**Migration file:** `Migrations/20260425150431_AddProductImageUrl.cs`

```csharp
migrationBuilder.AddColumn<string>(
    name: "ImageUrl",
    table: "Products",
    type: "text",
    nullable: true);
```

**Apply with:**
```bash
dotnet ef database update
```

---

## Setup Checklist

- [ ] Fill in real SMTP credentials in `appsettings.json → Email`
- [ ] Run `dotnet ef database update` to apply the `AddProductImageUrl` migration
- [ ] Start with `dotnet watch run` from `backend-api/`


| Feature | Files Changed |
|---|---|
| Product Filtering | `DTOs/ProductDtos.cs`, `Controllers/ProductsController.cs` |
| In-Memory Caching (5 min) | `Program.cs`, `Controllers/ProductsController.cs` |
| Transaction on Registration | `Controllers/AuthController.cs` |
| Product Image Upload | `models/Product.cs`, `DTOs/ProductDtos.cs`, `Controllers/ProductsController.cs`, `Program.cs` |
| Email Confirmation | `Services/EmailSender.cs`, `Controllers/AuthController.cs`, `Program.cs`, `appsettings.json` |

---

## 1. Product Filtering

**Endpoint:** `GET /api/products`

All filter parameters are optional query strings. Mix and match any combination.

### Query Parameters

| Parameter | Type | Description |
|---|---|---|
| `name` | string | Partial, case-insensitive name match |
| `sku` | string | Exact, case-insensitive SKU match |
| `minPrice` | decimal | Minimum price (inclusive) |
| `maxPrice` | decimal | Maximum price (inclusive) |
| `categoryId` | int | Filter by category ID |

### Examples

```
GET /api/products                          → all products (cached)
GET /api/products?name=shirt               → products whose name contains "shirt"
GET /api/products?minPrice=10&maxPrice=50  → products priced between $10–$50
GET /api/products?categoryId=2             → products in category 2
GET /api/products?name=nike&minPrice=20    → combined filters
```

### Files Changed

**`DTOs/ProductDtos.cs`** — new `ProductFilterDto` class:

```csharp
public class ProductFilterDto
{
    public string? Name { get; set; }
    public string? SKU { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? CategoryId { get; set; }
}
```

**`Controllers/ProductsController.cs`** — `GetAll` now accepts `[FromQuery] ProductFilterDto filter` and chains `.Where()` clauses dynamically, only for fields that are actually provided.

---

## 2. In-Memory Caching (5 Minutes)

**Endpoint:** `GET /api/products` (no filters)

The full unfiltered product list is cached in memory for 5 minutes to avoid hitting the database on every request. Filtered requests always go directly to the database.

### How It Works

```
Request with no filters
  → Check cache → HIT  → return immediately (no DB call)
                → MISS → query DB → store in cache → return

Request with any filter
  → Always query DB (not cached)
```

### Files Changed

**`Program.cs`** — registers the cache service:
```csharp
builder.Services.AddMemoryCache();
```

**`Controllers/ProductsController.cs`** — cache logic in `GetAll`:
```csharp
private const string AllProductsCacheKey = "products_all";

// Return from cache if no filters and cache hit
if (!hasFilter && _cache.TryGetValue(AllProductsCacheKey, out List<ProductDto>? cached))
    return Ok(cached);

// After querying DB, store result for 5 minutes
if (!hasFilter)
    _cache.Set(AllProductsCacheKey, results, TimeSpan.FromMinutes(5));
```

> **Note:** The cache expires automatically after 5 minutes. There is no manual invalidation on writes — this keeps the implementation simple.

---

## 3. Transaction on User Registration

**Endpoints:** `POST /api/auth/register-customer`, `register-admin`, `register-vendor`

Registration now wraps both the user creation and role assignment in a single database transaction. If either step fails, the entire operation is rolled back — no orphaned users with missing roles.

### Before vs After

```
BEFORE (no transaction)
  CreateAsync succeeds   → user saved to DB
  AddToRoleAsync fails   → user stuck in DB with no role (broken state)

AFTER (with transaction)
  Begin transaction
  CreateAsync succeeds   → staged
  AddToRoleAsync fails   → rollback → nothing saved
  Both succeed           → commit → both saved atomically
```

### Files Changed

**`Controllers/AuthController.cs`** — `RegisterByRole` method:

```csharp
await using var tx = await _context.Database.BeginTransactionAsync();

var createResult = await _userManager.CreateAsync(user, model.Password);
if (!createResult.Succeeded)
{
    await tx.RollbackAsync();
    return BadRequest(...);
}

var roleResult = await _userManager.AddToRoleAsync(user, role);
if (!roleResult.Succeeded)
{
    await tx.RollbackAsync();
    return BadRequest(...);
}

await tx.CommitAsync();
```

`AppDbContext` is injected into `AuthController` so the transaction covers the same connection used by `UserManager`.

---

## 4. Product Image Upload

### Model

**`models/Product.cs`** — new optional field:
```csharp
public string? ImageUrl { get; set; }
```
Stores the relative URL of the saved file, e.g. `/uploads/products/abc123.jpg`.

### Uploading an Image

**Endpoint:** `POST /api/products` or `PUT /api/products/{id}`

Both endpoints accept `multipart/form-data`. In Swagger, a **Choose File** button appears alongside the other fields.

| Field | Type | Required |
|---|---|---|
| Name | string | Yes |
| SKU | string | Yes |
| Price | decimal | Yes |
| Stock | int | No |
| CategoryId | int | Yes |
| SupplierId | int | Yes |
| Image | file | No |

**Allowed types:** jpg, jpeg, png, gif, webp  
**Max size:** 5 MB

### Where Files Are Saved

```
wwwroot/
└── uploads/
    └── products/
        ├── 7142125c-dbb1-4154-97ef-ea059440616d.jpg
        └── ...
```

Files are saved under `wwwroot/uploads/products/` with a random GUID filename to prevent collisions. The relative URL is stored in the `ImageUrl` column.

### Accessing an Image

Because `app.UseStaticFiles()` was added to `Program.cs`, uploaded images are served directly as static files:

```
GET /uploads/products/7142125c-dbb1-4154-97ef-ea059440616d.jpg
```

### Files Changed

**`DTOs/ProductDtos.cs`** — `IFormFile? Image` added to `CreateProductDto` and `UpdateProductDto`.

**`Controllers/ProductsController.cs`** — `[Consumes("multipart/form-data")]` + `[FromForm]` on Create and Update; `SaveImageAsync` private helper handles validation and file saving.

**`Program.cs`** — `app.UseStaticFiles()` added to serve `wwwroot`.

**`Migrations/…AddProductImageUrl`** — EF migration adds the column:
```sql
ALTER TABLE "Products" ADD "ImageUrl" text;
```

---

## 5. Email Confirmation on Registration

### Flow

```
1. User registers → transaction commits → confirmation email sent
2. User clicks link in email → GET /api/auth/confirm-email?userId=...&token=...
3. EmailConfirmed = true set in database
4. User can now log in
```

### New File: `Services/EmailSender.cs`

Contains three things:

**`EmailOptions`** — bound from `appsettings.json → "Email"` section:
```csharp
public class EmailOptions
{
    public string SmtpHost { get; set; }
    public int SmtpPort { get; set; }   // default 587
    public string From { get; set; }
    public string Password { get; set; }
    public bool EnableSsl { get; set; } // default true
}
```

**`IEmailSender`** — interface for easy swapping (e.g. to SendGrid):
```csharp
public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
}
```

**`SmtpEmailSender`** — implementation using `System.Net.Mail.SmtpClient`. SMTP errors are logged but do not throw — a mail server failure will not break registration.

### Configuration: `appsettings.json`

```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "From": "your-email@gmail.com",
  "Password": "your-app-password",
  "EnableSsl": true
}
```

> For Gmail, use an **App Password** — go to Google Account → Security → 2-Step Verification → App passwords.

### New Endpoint: `GET /api/auth/confirm-email`

| Query Param | Description |
|---|---|
| `userId` | The user's ID |
| `token` | Base64Url-encoded confirmation token |

**Success response:**
```json
{ "message": "Email confirmed successfully. You can now log in." }
```

### Login Guard

`POST /api/auth/login` now checks `EmailConfirmed` before the password:

```
Login attempt
  → User not found          → 401
  → EmailConfirmed = false  → 401 "Please confirm your email address before logging in."
  → Wrong password          → 401
  → All checks pass         → 200 with JWT
```

### Files Changed

**`Services/EmailSender.cs`** — new file (described above).

**`Controllers/AuthController.cs`** — `IEmailSender` injected; `SendConfirmationEmailAsync` called after transaction commit; `ConfirmEmail` endpoint added; login guard added.

**`Program.cs`** — services registered:
```csharp
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
```

---

## Setup Checklist

To get all features working on a fresh environment:

- [ ] Fill in SMTP credentials in `appsettings.json → Email`
- [ ] Run `dotnet ef database update` to apply the `AddProductImageUrl` migration
- [ ] Ensure `wwwroot/uploads/products/` directory exists (created automatically on first upload)
- [ ] Start with `dotnet watch run` from `backend-api/`
