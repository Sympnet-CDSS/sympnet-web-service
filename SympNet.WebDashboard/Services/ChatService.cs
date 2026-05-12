using Microsoft.AspNetCore.SignalR.Client;

namespace SympNet.WebDashboard.Services;

public class ChatService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // ── Events ────────────────────────────────────────────────────────────
    public event Func<object, Task>? OnMessageReceived;
    public event Func<object, Task>? OnIncomingCall;
    public event Func<string, Task>? OnCallAccepted;
    public event Func<string, Task>? OnCallRejected;
    public event Func<string, Task>? OnCallEnded;
    public event Func<object, Task>? OnUserTyping;
    public event Func<object, Task>? OnReceiveOffer;
    public event Func<object, Task>? OnReceiveAnswer;
    public event Func<object, Task>? OnReceiveIceCandidate;
    public event Func<string, Task>? OnUserJoined;

    public HubConnectionState State =>
        _hubConnection?.State ?? HubConnectionState.Disconnected;

    public ChatService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection?.State == HubConnectionState.Connected) return;

        var apiUrl = _config["ApiSettings:BaseUrl"] ?? "http://localhost:5057";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiUrl}/chatHub", options =>
            {
                var token = _httpContextAccessor.HttpContext?.Request.Cookies["auth_token"];
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        // ── Register all handlers here — NOT in individual components ────

        _hubConnection.On<object>("ReceiveMessage", async msg =>
        {
            if (OnMessageReceived != null) await OnMessageReceived(msg);
        });

        _hubConnection.On<object>("IncomingCall", async data =>
        {
            if (OnIncomingCall != null) await OnIncomingCall(data);
        });

        _hubConnection.On<object>("CallAccepted", async data =>
        {
            if (OnCallAccepted != null) await OnCallAccepted(data.ToString()!);
        });

        _hubConnection.On<string>("CallRejected", async sessionId =>
        {
            if (OnCallRejected != null) await OnCallRejected(sessionId);
        });

        _hubConnection.On<string>("CallEnded", async sessionId =>
        {
            if (OnCallEnded != null) await OnCallEnded(sessionId);
        });

        _hubConnection.On<object>("UserTyping", async data =>
        {
            if (OnUserTyping != null) await OnUserTyping(data);
        });

        _hubConnection.On<object>("ReceiveOffer", async data =>
        {
            if (OnReceiveOffer != null) await OnReceiveOffer(data);
        });

        _hubConnection.On<object>("ReceiveAnswer", async data =>
        {
            if (OnReceiveAnswer != null) await OnReceiveAnswer(data);
        });

        _hubConnection.On<object>("ReceiveIceCandidate", async data =>
        {
            if (OnReceiveIceCandidate != null) await OnReceiveIceCandidate(data);
        });

        _hubConnection.On<string>("UserJoined", async userId =>
        {
            if (OnUserJoined != null) await OnUserJoined(userId);
        });

        await _hubConnection.StartAsync();
    }

    // ── Send methods ──────────────────────────────────────────────────────

    public async Task SendMessage(string consultationId, string content, bool isVoice = false)
    {
        await Invoke("SendMessage", consultationId, content, isVoice);
    }

    public async Task JoinConsultation(string consultationId)
    {
        await Invoke("JoinConsultation", consultationId);
    }

    public async Task LeaveConsultation(string consultationId)
    {
        await Invoke("LeaveConsultation", consultationId);
    }

    public async Task SendTyping(string consultationId, bool isTyping)
    {
        await Invoke("SendTyping", consultationId, isTyping);
    }

    public async Task SendOffer(string targetUserId, string sdp)
    {
        await Invoke("SendOffer", targetUserId, sdp);
    }

    public async Task SendAnswer(string targetUserId, string sdp)
    {
        await Invoke("SendAnswer", targetUserId, sdp);
    }

    public async Task SendIceCandidate(string targetUserId, string candidate, string sdpMid, int sdpMLineIndex)
    {
        await Invoke("SendIceCandidate", targetUserId, candidate, sdpMid, sdpMLineIndex);
    }

    public async Task RejectCall(string targetUserId, string sessionId)
    {
        await Invoke("RejectCall", targetUserId, sessionId);
    }

    public async Task EndCall(string targetUserId, string sessionId)
    {
        await Invoke("EndCall", targetUserId, sessionId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task Invoke(string method, params object[] args)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.InvokeCoreAsync(method, args);
        else
            Console.WriteLine($"[SignalR] Cannot invoke '{method}' — not connected");
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
            await _hubConnection.DisposeAsync();
    }
}