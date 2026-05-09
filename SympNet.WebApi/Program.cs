using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SympNet.WebApi.Data;
using SympNet.WebApi.Hubs;
using SympNet.WebApi.Services;
using SympNet.WebApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Contrôleurs
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SympNet_Super_Secret_Key_2026_TEKup_Very_Long_For_256bits_Minimum!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SympNet";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SympNetUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DoctorOnly", policy => policy.RequireRole("Doctor"));
    options.AddPolicy("PatientOnly", policy => policy.RequireRole("Patient"));
    options.AddPolicy("DoctorOrAdmin", policy => policy.RequireRole("Doctor", "Admin"));
});

// Services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IAIService, AIService>();
// builder.Services.AddScoped<INotificationService, NotificationService>();

// HTTP Client for AI Service
builder.Services.AddHttpClient<IAIService, AIService>(client =>
{
    var aiUrl = builder.Configuration["AIService:Url"] ?? "http://localhost:8001";
    client.BaseAddress = new Uri(aiUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Allow serving uploads (avatars)
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// SignalR Hubs
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<VideoCallHub>("/hubs/videocall");

// Controllers
app.MapControllers();

// Migration et Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    
    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE \"Ordonnances\" ADD COLUMN IF NOT EXISTS \"OrdonnanceCode\" text NOT NULL DEFAULT '';");
        db.Database.ExecuteSqlRaw("ALTER TABLE \"Ordonnances\" ADD COLUMN IF NOT EXISTS \"Status\" integer NOT NULL DEFAULT 0;");
        db.Database.ExecuteSqlRaw("ALTER TABLE \"Ordonnances\" ADD COLUMN IF NOT EXISTS \"HasAIAlerts\" boolean NOT NULL DEFAULT FALSE;");
        db.Database.ExecuteSqlRaw("ALTER TABLE \"Ordonnances\" ADD COLUMN IF NOT EXISTS \"AIAlertsJson\" text NULL;");
        db.Database.ExecuteSqlRaw("ALTER TABLE \"Ordonnances\" ADD COLUMN IF NOT EXISTS \"DoctorConfirmed\" boolean NOT NULL DEFAULT FALSE;");
        db.Database.ExecuteSqlRaw("ALTER TABLE \"Ordonnances\" ADD COLUMN IF NOT EXISTS \"ConfirmedAt\" timestamp with time zone NULL;");
        db.Database.ExecuteSqlRaw("ALTER TABLE \"Ordonnances\" ADD COLUMN IF NOT EXISTS \"PdfPath\" text NULL;");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Schema update skipped: {ex.Message}");
    }
    
    // Seed admin si nécessaire
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        var admin = new User
        {
            Email = "admin@sympnet.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            IsActive = true,
            IsEmailVerified = true,
            FullName = "Administrateur SympNet"
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        Console.WriteLine("✓ Admin user created: admin@sympnet.com / Admin@123");
    }
}

app.Run();