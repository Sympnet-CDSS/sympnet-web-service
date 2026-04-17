using SympNet.WebDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Ajout des services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ApiService>();
// builder.Services.AddScoped<EmailService>(); ← SUPPRIME CETTE LIGNE

builder.Services.AddAuthentication().AddCookie("AuthCookie", options =>
{
    options.Cookie.Name = "auth_token";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/login";
});

builder.Services.AddAuthorization();

// Configuration anti-forgery
builder.Services.AddAntiforgery(options => 
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<SympNet.WebDashboard.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();