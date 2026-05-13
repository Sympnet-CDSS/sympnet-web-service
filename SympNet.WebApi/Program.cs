using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SympNet.WebApi.Data;
using SympNet.WebApi.Hubs;
using SympNet.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, NameUserIdProvider>();

// ... (configuration continue)


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SympNet_Super_Secret_Key_2026_TEKup_Very_Long_For_256bits_Minimum!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // ✅ OBLIGATOIRE : permet à SignalR de lire le token depuis la query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (path.StartsWithSegments("/hubs"))
                {
                    Console.WriteLine($"[SignalR-Auth] Connection attempt to {path}. Token found in query: {!string.IsNullOrEmpty(accessToken)}");
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();

// ✅ CORS corrigé — AllowCredentials() requis par SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
        policy
            .SetIsOriginAllowed(_ => true) // ✅ Autoriser toutes les origines en développement
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()); // ← obligatoire pour WebSocket
});

var app = builder.Build();

// Seed admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var adminEmail    = builder.Configuration["Admin:Email"]    ?? "admin@sympnet.com";
    var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin123!";

    db.Database.EnsureCreated();

    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        db.Users.Add(new SympNet.WebApi.Models.User
        {
            Id           = Guid.NewGuid(),
            Email        = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role         = "Admin",
            IsActive     = true,
            FullName     = "Super Administrateur",
            CreatedAt    = DateTime.UtcNow
        });
        db.SaveChanges();
        Console.WriteLine($"✅ ADMIN CRÉÉ — Email: {adminEmail}");
    }
    else
    {
        Console.WriteLine("✅ L'administrateur existe déjà.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // ✅ Désactivé pour éviter les blocages SignalR en développement local

// ✅ CORS avant Authentication
app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

// ✅ LIGNE MANQUANTE — hub de notifications
app.MapHub<NotificationHub>("/hubs/notifications");


app.Run();

public class NameUserIdProvider : Microsoft.AspNetCore.SignalR.IUserIdProvider
{
    public string? GetUserId(Microsoft.AspNetCore.SignalR.HubConnectionContext connection)
    {
        return connection.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
            ?? connection.User?.FindFirst("sub")?.Value;
    }
}