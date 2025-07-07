using ChatServer.SignalR.Hubs;
using ChatServer.Services;
using ChatServer.SignalR;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using ChatServer.Configs;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Data;
using Npgsql;
using ChatServer.Repositories.Messenger;
using ChatServer.Applications;
using ChatServer.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


// ===== 1. Service registrations =====
builder.Services.AddControllers();
builder.Services.AddOpenApi();                // built-in OpenAPI (ASP.NET 9)

builder.Services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(connectionString));

builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddScoped<IMessageRepo, MessageRepo>();
builder.Services.AddScoped<IUserRepo, UserRepo>();
builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();

builder.Services.AddScoped<IChatClientNotifier, SignalRChatClientNotifier>();



builder.Services.AddAuthentication(options =>
{
    // Đặt scheme xác thực mặc định là JwtBearer
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Đây là phần cấu hình để server xác thực token
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Kiểm tra nhà phát hành (Issuer)
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Lấy từ appsettings.json

        // Kiểm tra bên nhận (Audience)
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"], // Lấy từ appsettings.json

        // Kiểm tra thời gian sống của token
        ValidateLifetime = true,

        // Kiểm tra và xác thực chữ ký của token
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),

        // Yêu cầu token phải có thời gian hết hạn
        RequireExpirationTime = true,

        // Không bù trừ thời gian khi kiểm tra token hết hạn
        ClockSkew = TimeSpan.Zero
    };

    // ✨ PHẦN QUAN TRỌNG NHẤT: Đọc token từ cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Cố gắng đọc token từ cookie có tên là "access_token"
            // Tên này phải khớp với tên cookie bạn đặt ở phía Next.js
            context.Request.Cookies.TryGetValue("access_token", out var token);

            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("AllowNextApp", policy =>
    {
        // 1) Cho phép origin local của Next.js
        policy.WithOrigins("http://localhost:3000")

              .SetIsOriginAllowed(origin =>
                {
                    var uri = new Uri(origin);

                    // Cho phép localhost (mọi port) hoặc *.aistudio.com.vn
                    return uri.IsLoopback                             // 127.0.0.1, localhost, v.v.
                        || uri.Host.EndsWith(".aistudio.com.vn", StringComparison.OrdinalIgnoreCase);
                })

              // 3) Các thiết lập CORS khác
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddSignalR();

builder.Services
       .AddOptions<RabbitMQOptions>()
       .Bind(builder.Configuration.GetSection("RabbitMQ"))
       .ValidateDataAnnotations();   // tùy chọn

// RabbitMQ 7.x – tạo kết nối singleton
builder.Services.AddSingleton<IConnection>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<RabbitMQOptions>>().Value;

    var factory = new ConnectionFactory
    {
        HostName = opts.HostName,
        UserName = opts.UserName,
        Password = opts.Password
    };

    // singleton cần sync
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddHostedService<RabbitMQConsumerService>();
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

// ===== 2. Build app =====
var app = builder.Build();

// ===== 3. Middleware / pipeline =====
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();         // /openapi.json + Swagger UI tích hợp
}

app.UseHttpsRedirection();
app.UseCors("AllowNextApp");
app.UseAuthentication();
app.UseAuthorization();

// ===== 4. Routing =====
app.MapControllers();
app.MapHub<ChatHub>("/chathub").RequireCors("AllowNextApp"); ;

// demo endpoint
var summaries = new[]
{
    "Freezing","Bracing","Chilly","Cool","Mild",
    "Warm","Balmy","Hot","Sweltering","Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

// ===== 5. Record type =====
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
