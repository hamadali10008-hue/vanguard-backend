using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Saas.Api.Services;
using Saas.Application.Interfaces;
using Saas.Application.Services;
using Saas.Appplication.Services;
using Saas.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

// 1. THE NET CORE CURE
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
var builder = WebApplication.CreateBuilder(args);

// --- 2. DATABASE CONFIGURATION ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetService<ApplicationDbContext>()!);

// --- 3. CRYPTOGRAPHIC JWT SECURITY ARCHITECTURE ---
const string securityMasterKey = "8KcvKmWDF2sqnXi5i4JFNRRQzLUG/QUzDJe7eIJ6XFg=";
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
        ValidIssuer = "SaasApi",
        ValidAudience = "SaasFrontend",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityMasterKey)),
        RoleClaimType = "role"
    };
});

// --- 4. CORS CONTROL GATEWAYS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://saasvanguard.vercel.app")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// --- 5. DEPENDENCY INJECTION ENGINE ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Service Registrations
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// --- 🛠️ AUTOMATED DATABASE CREATION & MIGRATION ENGINE ---
// This runs asynchronously on startup to fix Error 4060 inside AWS RDS automatically
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // This checks for pending migrations and applies them. 
        // If the database does not exist, it creates it first.
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An automated exception occurred while provisioning or migrating SaasDb.");
    }
}

// --- 6. HTTP REQUEST PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    // Only force HTTPS redirection locally.
    app.UseHttpsRedirection();
}

// CORS must execute BEFORE Authentication/Authorization
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
