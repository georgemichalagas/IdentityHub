using IdentityHub.Api.Data;
using IdentityHub.Api.Models;
using IdentityHub.Api.Services;
using IdentityHub.Api.Mappings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Runtime.CompilerServices;
using System.Text;
using IdentityHub.Api.Controllers.Grpc;

[assembly: InternalsVisibleTo("IdentityHub.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    "Host=localhost;Database=identityhub;Username=postgres;Password=postgres";

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity - Environment specific configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    var isProduction = builder.Environment.IsProduction();
    
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = isProduction;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = isProduction ? 8 : 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings - stricter in production
    var securityConfig = builder.Configuration.GetSection("Security");
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
        securityConfig.GetValue<int>("LockoutTimeSpan", isProduction ? 30 : 5));
    options.Lockout.MaxFailedAccessAttempts = 
        securityConfig.GetValue<int>("MaxFailedAccessAttempts", isProduction ? 3 : 5);
    options.Lockout.AllowedForNewUsers = 
        securityConfig.GetValue<bool>("EnableAccountLockout", true);

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Email confirmation - required in production
    options.SignIn.RequireConfirmedEmail = 
        securityConfig.GetValue<bool>("RequireEmailConfirmation", isProduction);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "DefaultSecretKeyForDevelopmentOnlyChangeInProduction123456789";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "IdentityHub",
        ValidAudience = jwtSettings["Audience"] ?? "IdentityHubClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin"));
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();

// gRPC Services
builder.Services.AddGrpc();
builder.Services.AddScoped<GrpcUserService>();
builder.Services.AddScoped<GrpcAuthService>();

// CORS - Environment specific
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        var corsSettings = builder.Configuration.GetSection("CorsSettings");
        var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000", "https://localhost:3000" };
            
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IdentityHub API",
        Version = "v1",
        Description = "A comprehensive authentication service API using ASP.NET Core Identity and JWT",
        Contact = new OpenApiContact
        {
            Name = "IdentityHub",
            Email = "support@identityhub.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IdentityHub API V1");
        c.RoutePrefix = string.Empty; // Swagger UI at app's root
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<GrpcUserService>();
app.MapGrpcService<GrpcAuthService>();

// Initialize database and seed roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Seed roles
        await SeedRolesAsync(roleManager);
        
        // Seed admin user in development
        if (app.Environment.IsDevelopment())
        {
            await SeedAdminUserAsync(userManager);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Helper methods for seeding
static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    var roles = new[] { "Admin", "User" };
    
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
{
    var adminEmail = "admin@identityhub.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

public partial class Program { }
