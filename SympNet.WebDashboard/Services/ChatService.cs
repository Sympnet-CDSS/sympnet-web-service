using Microsoft.AspNetCore.SignalR.Client;

namespace SympNet.WebDashboard.Services;

public class ChatService
{
    private HubConnection? _hubConnection;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ChatService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task ConnectAsync()
    {
        var apiUrl = _config["ApiSettings:BaseUrl"] ?? "http://localhost:5057";
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiUrl}/chatHub", options =>
            {
                // Récupérer le token du cookie
                var token = _httpContextAccessor.HttpContext?.Request.Cookies["auth_token"];
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();
        
        await _hubConnection.StartAsync();
    }
    
    public async Task SendMessage(string receiverId, string content)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendMessage", receiverId, content);
        }
    }
    
    public void OnMessageReceived(Func<object, Task> handler)
    {
        _hubConnection?.On<object>("ReceiveMessage", handler);
    }
    
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}