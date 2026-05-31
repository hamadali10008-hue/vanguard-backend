using LMS_System.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SignalR must be registered
builder.Services.AddSignalR(options => {
    options.EnableDetailedErrors = true;
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Pings the browser to keep it awake
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. UPDATED CORS POLICY
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173") // Your Vite React Port
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // CRITICAL: Required for SignalR/WebSockets
        });
});

var app = builder.Build();

// 3. MIDDLEWARE ORDER IS CRITICAL
// UseCors must come BEFORE MapHub and MapControllers
app.UseCors("AllowReactApp");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// 4. MAP ENDPOINTS
app.MapControllers();
app.MapHub<LMS_System.Hubs.ChatHub>("/chatHub");

app.Run();