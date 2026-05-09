using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SympNet.WebApi.Data;
using SympNet.WebApi.Hubs;
using SympNet.WebApi.Services;
using SympNet.WebApi.Hubs;

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
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddCors();
builder.Services.AddSignalR();
builder.Services.AddCors();

var app = builder.Build();

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
        
        Console.WriteLine(" ADMIN CRÉÉ AVEC SUCCÈS !");
        Console.WriteLine($"   Email: {adminEmail}");
        Console.WriteLine($"   Mot de passe: {adminPassword}");
    }
    else
    {
        Console.WriteLine(" L'administrateur existe déjà.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();