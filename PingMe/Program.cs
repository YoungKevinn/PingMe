using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PingMe.BackgroundJobs;
using PingMe.Data;
using PingMe.Hubs;
using PingMe.Middleware;
using PingMe.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 46)),
        mySql => mySql.EnableRetryOnFailure(3)));

var jwtSecret   = builder.Configuration["Jwt:Secret"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 1024 * 1024;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PingMe API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }
    });
});

builder.Services.AddDirectoryBrowser();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();

builder.Services.Configure<PingMe.Settings.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ForgotPasswordService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<ISavedMessageService, SavedMessageService>();
builder.Services.AddScoped<IReadReceiptService, ReadReceiptService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ISnippetService, SnippetService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IIocService, IocService>();
builder.Services.AddScoped<IPentestFindingService, PentestFindingService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IGroupTaskService, GroupTaskService>();
builder.Services.AddScoped<IOneTimeSecretService, OneTimeSecretService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddSingleton<ISignalRConnectionTracker, SignalRConnectionTracker>();
builder.Services.AddHostedService<MessageExpiryJob>();
builder.Services.AddHostedService<ReminderDispatchJob>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PingMe API v1");
        c.DisplayRequestDuration();
    });
}

app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JwtRevocationMiddleware>();
app.UseMiddleware<AuditLogMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
