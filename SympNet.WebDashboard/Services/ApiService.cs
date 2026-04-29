using System.Net.Http.Json;

namespace SympNet.WebDashboard.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;

    public ApiService(HttpClient http, IHttpContextAccessor httpContextAccessor, IConfiguration config)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _config = config;
    }

    private string GetApiUrl() => _config["ApiSettings:BaseUrl"] ?? "http://localhost:5002";

    private async Task<string?> GetTokenAsync()
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            return _httpContextAccessor.HttpContext.Request.Cookies["auth_token"];
        }
        return null;
    }

    private async Task<HttpClient> GetAuthorizedClientAsync()
    {
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return _http;
    }

    public async Task<LoginResponse?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync($"{GetApiUrl()}/api/auth/login", new { email, password });
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            
            if (result != null && _httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Response.Cookies.Append("auth_token", result.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }
            return result;
        }
        return null;
    }

    public async Task LogoutAsync()
    {
        if (_httpContextAccessor.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Response.Cookies.Delete("auth_token");
        }
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.GetAsync($"{GetApiUrl()}/api/admin/users");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new();
        }
        return new();
    }

    public async Task<StatsDto?> GetStatsAsync()
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.GetAsync($"{GetApiUrl()}/api/admin/stats");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<StatsDto>();
        }
        return null;
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserRequest request)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync($"{GetApiUrl()}/api/admin/users", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }
        return null;
    }

    public async Task<bool> ToggleUserActiveAsync(Guid userId)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.PatchAsync($"{GetApiUrl()}/api/admin/users/{userId}/toggle-active", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var client = await GetAuthorizedClientAsync();
        var response = await client.DeleteAsync($"{GetApiUrl()}/api/admin/users/{userId}");
        return response.IsSuccessStatusCode;
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class StatsDto
{
    public int TotalUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Patient";
    public string? FullName { get; set; }
}
