using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SympNet.WebApi.Data;
using SympNet.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SympNet API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

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
            ValidateIssuer = true,
            ValidateAudience = true,
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

var app = builder.Build();

// === CRÉATION AUTO DE L'ADMIN (UNE SEULE FOIS) ===
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
        
        Console.WriteLine("========================================");
        Console.WriteLine("✅ ADMIN CRÉÉ AVEC SUCCÈS !");
        Console.WriteLine($"   Email: {adminEmail}");
        Console.WriteLine($"   Mot de passe: {adminPassword}");
        Console.WriteLine("========================================");
    }
    else
    {
        Console.WriteLine("ℹ️ L'administrateur existe déjà.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();