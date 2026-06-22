using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor.Services;
using PingMe.Frontend.Components;
using PingMe.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped<IocService>();
builder.Services.AddScoped<IocNavStateService>();
builder.Services.AddScoped<PentestFindingService>();
builder.Services.AddScoped<GroupTaskService>();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.PopoverOptions.ThrowOnDuplicateProvider = false;
});

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Auth
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ForgotPasswordService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<UnreadStateService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<MessageCacheService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<ReactionService>();
builder.Services.AddScoped<SavedMessageService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<ReadReceiptService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<BlockService>();
builder.Services.AddScoped<SnippetService>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<FriendService>();
builder.Services.AddScoped<OneTimeSecretService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddScoped<PollService>();

// SignalR hub (singleton vì cần dùng across components)
builder.Services.AddSingleton<ChatHubService>();

// WebRTC
builder.Services.AddScoped<WebRTCService>();

try 
{
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    var js = builder.Services.BuildServiceProvider().GetRequiredService<IJSRuntime>();
    await js.InvokeVoidAsync("console.error", ex.ToString());
    await js.InvokeVoidAsync("eval", $"document.body.innerHTML = '<div style=\"color:red;padding:20px;\"><h2>Fatal Startup Error</h2><pre>' + `{ex.ToString().Replace("`", "\\`")}` + '</pre></div>'");
}
