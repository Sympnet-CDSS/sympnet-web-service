using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SympNet.WebApi.Data;
using SympNet.WebApi.Hubs;
using SympNet.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database - PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SympNet_Super_Secret_Key_2026_TEKup!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        
        // Important pour SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSignalR();
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// MIDDLEWARE POUR AUTH TEMPORAIRE (POUR TEST)
app.Use(async (context, next) =>
{
    // Pour les requêtes vers chat, ajouter un utilisateur par défaut
    if (context.Request.Path.StartsWithSegments("/api/chat") || 
        context.Request.Path.StartsWithSegments("/chatHub"))
    {
        var userId = "11111111-1111-1111-1111-111111111111";
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId),
            new Claim(ClaimTypes.Role, "Doctor")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        context.User = new ClaimsPrincipal(identity);
    }
    
    await next();
});

// app.UseAuthentication(); // COMMENTÉ pour test
// app.UseAuthorization();  // COMMENTÉ pour test

app.MapHub<ChatHub>("/chatHub");
app.MapHub<WebRTCHub>("/webrtchub");
app.MapControllers();

// Créer admin par défaut
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@sympnet.com";
    var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin123!";
    
    db.Database.EnsureCreated();
    
    var adminExists = db.Users.Any(u => u.Role == "Admin");
    
    if (!adminExists)
    {
        var adminUser = new SympNet.WebApi.Models.User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = "Admin",
            IsActive = true,
            FullName = "Super Administrateur",
            CreatedAt = DateTime.UtcNow
        };
        
        db.Users.Add(adminUser);
        db.SaveChanges();
        
        Console.WriteLine("ADMIN CRÉÉ AVEC SUCCÈS !");
        Console.WriteLine($"   Email: {adminEmail}");
        Console.WriteLine($"   Mot de passe: {adminPassword}");
    }
}

app.Run();