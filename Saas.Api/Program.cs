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

// 1. THE NET CORE CURE: Clear default legacy mapping schemas so clean strings like "TenantId" pass through unrenamed
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// --- 2. DATABASE CONFIGURATION ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetService<ApplicationDbContext>()!);

// --- 3. CRYPTOGRAPHIC JWT SECURITY ARCHITECTURE ---
// 💡 Real, secure 256-bit signing token key string (64 characters = 32 bytes)
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
        RoleClaimType = "role" // 💡 Keeps backend [Authorize(Roles="Admin")] perfectly mapped to our lowercase frontend string
    };
});

// --- 4. CORS CONTROL GATEWAYS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJS",
        policy =>
        {
            policy.WithOrigins("https://vanguard-frontend-p0og6640o-hamadali10008-hues-projects.vercel.app/")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Standard requirement for enterprise state sharing
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

// --- 6. HTTP REQUEST PIPELINE (THE EXECUTION FLOW ORDER IS CRITICAL HERE) ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 💡 CORS must execute BEFORE Authentication/Authorization so pre-flight browser checks don't throw 401s
app.UseCors("AllowNextJS");

app.UseAuthentication(); // 1. Who are you? (Parses the token)
app.UseAuthorization();  // 2. What are you allowed to see? (Checks Roles)

app.MapControllers();

app.Run();
